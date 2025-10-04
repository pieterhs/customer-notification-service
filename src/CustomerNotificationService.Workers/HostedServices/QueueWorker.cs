using CustomerNotificationService.Application.Interfaces;
using CustomerNotificationService.Domain.Entities;
using CustomerNotificationService.Domain.Enums;
using CustomerNotificationService.Infrastructure.Data;
using CustomerNotificationService.Infrastructure.Providers;
using CustomerNotificationService.Workers.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Scriban;

namespace CustomerNotificationService.Workers.HostedServices;

public class QueueWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<QueueWorker> _logger;
    private readonly RetryPolicyOptions _retryPolicy;

    public QueueWorker(IServiceProvider services, ILogger<QueueWorker> logger, IOptions<RetryPolicyOptions> retryPolicy)
    {
        _services = services;
        _logger = logger;
        _retryPolicy = retryPolicy.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("QueueWorker started with retry policy: MaxAttempts={MaxAttempts}, BaseBackoff={BaseBackoff}s, MaxBackoff={MaxBackoff}s", 
            _retryPolicy.MaxAttempts, _retryPolicy.BaseBackoffSeconds, _retryPolicy.MaxBackoffSeconds);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessQueueItemsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing queue items");
            }
            
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task ProcessQueueItemsAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var queueRepository = scope.ServiceProvider.GetRequiredService<IQueueRepository>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var providers = scope.ServiceProvider.GetServices<INotificationProvider>();
    var auditLogger = scope.ServiceProvider.GetRequiredService<IAuditLogger>();

        // Dequeue only ready items (Status='Queued' and NextAttemptAt check)
        var queueItem = await DequeueReadyItemAsync(dbContext, cancellationToken);
        if (queueItem == null) 
        {
            _logger.LogDebug("No ready queue items to process");
            return;
        }

        _logger.LogDebug("Processing queue item {QueueItemId} for notification {NotificationId} (attempt {AttemptCount})", 
            queueItem.Id, queueItem.NotificationId, queueItem.AttemptCount);

        try
        {
            var notification = await dbContext.Notifications
                .FirstOrDefaultAsync(n => n.Id == queueItem.NotificationId, cancellationToken);
                
            if (notification == null)
            {
                _logger.LogWarning("Notification {NotificationId} not found, marking queue item as processed", queueItem.NotificationId);
                await MarkQueueItemProcessedAsync(dbContext, queueItem, cancellationToken);
                return;
            }

            // Render template if needed
            await RenderTemplateAsync(notification, dbContext, cancellationToken);

            // Find and invoke provider
            var provider = providers.FirstOrDefault(p => p.Channel.Equals(notification.Channel.ToString(), StringComparison.OrdinalIgnoreCase));
            if (provider == null)
            {
                _logger.LogError("No provider found for channel {Channel}, marking as failed", notification.Channel);
                await HandleDeliveryFailureAsync(dbContext, queueItem, notification, auditLogger, "No provider found for channel", cancellationToken);
                return;
            }

            // Attempt delivery
            var deliverySuccess = await provider.SendAsync(notification, cancellationToken);
            
            if (deliverySuccess)
            {
                await HandleDeliverySuccessAsync(dbContext, queueItem, notification, auditLogger, cancellationToken);
            }
            else
            {
                await HandleDeliveryFailureAsync(dbContext, queueItem, notification, auditLogger, "Provider delivery failed", cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing notification {NotificationId}", queueItem.NotificationId);
            await HandleDeliveryFailureAsync(dbContext, queueItem, null, auditLogger, $"Exception: {ex.Message}", cancellationToken);
        }
    }

    private async Task<NotificationQueueItem?> DequeueReadyItemAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        
        try
        {
            using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            
            NotificationQueueItem? item;
            if (dbContext.Database.IsNpgsql())
            {
                // Use FOR UPDATE SKIP LOCKED for safe concurrent dequeuing
                item = await dbContext.NotificationQueue
                    .FromSqlInterpolated($@"SELECT * FROM ""NotificationQueue"" WHERE ""JobStatus"" = 'Queued' AND ""ReadyAt"" <= {now} AND (""NextAttemptAt"" IS NULL OR ""NextAttemptAt"" <= {now}) ORDER BY ""ReadyAt"" LIMIT 1 FOR UPDATE SKIP LOCKED")
                    .AsTracking()
                    .FirstOrDefaultAsync(cancellationToken);
            }
            else
            {
                item = await dbContext.NotificationQueue
                    .Where(q => q.JobStatus == "Queued" && q.ReadyAt <= now && (q.NextAttemptAt == null || q.NextAttemptAt <= now))
                    .OrderBy(q => q.ReadyAt)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            if (item != null)
            {
                item.JobStatus = "Processing";
                item.AttemptCount++;
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return item;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Transactions are not supported"))
        {
            // Fallback for in-memory database
            var item = await dbContext.NotificationQueue
                .Where(q => q.JobStatus == "Queued" && q.ReadyAt <= now && (q.NextAttemptAt == null || q.NextAttemptAt <= now))
                .OrderBy(q => q.ReadyAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (item != null)
            {
                item.JobStatus = "Processing";
                item.AttemptCount++;
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return item;
        }
    }

    private async Task HandleDeliverySuccessAsync(AppDbContext dbContext, NotificationQueueItem queueItem, Notification notification, IAuditLogger auditLogger, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Notification {NotificationId} delivered successfully", notification.Id);

        // Update notification status
        notification.Status = NotificationStatus.Sent;
        notification.SentAt = DateTimeOffset.UtcNow;

        // Mark queue item as processed
        queueItem.JobStatus = "Processed";
        queueItem.CompletedAt = DateTimeOffset.UtcNow;

        // Log successful delivery attempt
        var deliveryAttempt = new DeliveryAttempt
        {
            Id = Guid.NewGuid(),
            NotificationId = notification.Id,
            AttemptedAt = DateTimeOffset.UtcNow,
            Success = true,
            Status = "Success",
            ResponseMessage = "Delivered successfully"
        };

    dbContext.DeliveryAttempts.Add(deliveryAttempt);
    await dbContext.SaveChangesAsync(cancellationToken);
    await auditLogger.LogAsync("NotificationSent", notification.Id, null);
    }

    private async Task HandleDeliveryFailureAsync(AppDbContext dbContext, NotificationQueueItem queueItem, Notification? notification, IAuditLogger auditLogger, string errorMessage, CancellationToken cancellationToken)
    {
        var attemptCount = queueItem.AttemptCount;
        var now = DateTimeOffset.UtcNow;
        
        // Log delivery attempt
        var deliveryAttempt = new DeliveryAttempt
        {
            Id = Guid.NewGuid(),
            NotificationId = queueItem.NotificationId,
            AttemptedAt = now,
            Success = false,
            ErrorMessage = errorMessage
        };

        if (attemptCount >= _retryPolicy.MaxAttempts)
        {
            // Exceeded max attempts - mark as permanently failed
            _logger.LogWarning("Notification {NotificationId} failed permanently after {AttemptCount} attempts", 
                queueItem.NotificationId, attemptCount);

            if (notification != null)
            {
                notification.Status = NotificationStatus.Failed;
            }

            queueItem.JobStatus = "Failed";
            queueItem.CompletedAt = now;

            deliveryAttempt.Status = "Failed";
            deliveryAttempt.ResponseMessage = $"Failed permanently after {attemptCount} attempts";

            await auditLogger.LogAsync("NotificationFailed", queueItem.NotificationId, errorMessage);
        }
        else
        {
            // Calculate backoff and retry
            var backoffTime = _retryPolicy.CalculateBackoff(attemptCount);
            var nextAttempt = now.Add(backoffTime);
            
            _logger.LogWarning("Notification {NotificationId} delivery failed (attempt {AttemptCount}/{MaxAttempts}), retrying in {BackoffSeconds}s", 
                queueItem.NotificationId, attemptCount, _retryPolicy.MaxAttempts, backoffTime.TotalSeconds);
            
            queueItem.JobStatus = "Queued";
            queueItem.NextAttemptAt = nextAttempt;
            
            deliveryAttempt.Status = "Failed (retry)";
            deliveryAttempt.RetryAfterSeconds = (int)backoffTime.TotalSeconds;
            deliveryAttempt.ResponseMessage = $"Failed, retrying after {backoffTime.TotalSeconds}s";
        }

        dbContext.DeliveryAttempts.Add(deliveryAttempt);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task MarkQueueItemProcessedAsync(AppDbContext dbContext, NotificationQueueItem queueItem, CancellationToken cancellationToken)
    {
        queueItem.JobStatus = "Processed";
        queueItem.CompletedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task RenderTemplateAsync(Notification notification, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(notification.TemplateKey)) return;

        var template = await dbContext.Templates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Key == notification.TemplateKey, cancellationToken);

        if (template == null)
        {
            _logger.LogWarning("Template {TemplateKey} not found for notification {NotificationId}", 
                notification.TemplateKey, notification.Id);
            return;
        }

        try
        {
            var payload = string.IsNullOrEmpty(notification.PayloadJson) 
                ? new { } 
                : System.Text.Json.JsonSerializer.Deserialize<object>(notification.PayloadJson);

            var subjectTemplate = Scriban.Template.Parse(template.Subject);
            var bodyTemplate = Scriban.Template.Parse(template.Body);

            notification.Subject = await subjectTemplate.RenderAsync(payload);
            notification.Body = await bodyTemplate.RenderAsync(payload);
            
            _logger.LogDebug("Template {TemplateKey} rendered successfully for notification {NotificationId}", 
                notification.TemplateKey, notification.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering template {TemplateKey} for notification {NotificationId}", 
                notification.TemplateKey, notification.Id);
            notification.Subject = template.Subject;
            notification.Body = template.Body;
        }
    }
}
