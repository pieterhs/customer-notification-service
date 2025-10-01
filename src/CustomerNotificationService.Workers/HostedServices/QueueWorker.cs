using CustomerNotificationService.Application.Interfaces;
using CustomerNotificationService.Domain.Entities;
using CustomerNotificationService.Domain.Enums;
using CustomerNotificationService.Infrastructure.Data;
using CustomerNotificationService.Infrastructure.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Scriban;

namespace CustomerNotificationService.Workers.HostedServices;

public class QueueWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<QueueWorker> _logger;
    private readonly QueueWorkerOptions _options;

    public QueueWorker(IServiceProvider services, ILogger<QueueWorker> logger, IOptions<QueueWorkerOptions> options)
    {
        _services = services;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("QueueWorker started");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessQueueItems(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing queue items");
            }
            
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task ProcessQueueItems(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var queueRepository = scope.ServiceProvider.GetRequiredService<IQueueRepository>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var providers = scope.ServiceProvider.GetServices<INotificationProvider>();

        var queueItem = await queueRepository.DequeueAsync(cancellationToken);
        if (queueItem == null) return;

        try
        {
            var notification = await dbContext.Notifications
                .FirstOrDefaultAsync(n => n.Id == queueItem.NotificationId, cancellationToken);
                
            if (notification == null)
            {
                _logger.LogWarning("Notification {NotificationId} not found", queueItem.NotificationId);
                await queueRepository.CompleteAsync(queueItem.Id, cancellationToken);
                return;
            }

            // Render template if needed
            await RenderTemplate(notification, dbContext, cancellationToken);

            // Send notification
            var provider = providers.FirstOrDefault(p => p.Channel.Equals(notification.Channel.ToString(), StringComparison.OrdinalIgnoreCase));
            if (provider == null)
            {
                throw new InvalidOperationException($"No provider found for channel {notification.Channel}");
            }

            var success = await provider.SendAsync(notification, cancellationToken);

            // Record delivery attempt
            var attempt = new DeliveryAttempt
            {
                Id = Guid.NewGuid(),
                NotificationId = notification.Id,
                AttemptedAt = DateTimeOffset.UtcNow,
                Success = success,
                ResponseMessage = success ? "Sent successfully" : "Send failed",
                Status = success ? "Success" : "Failed"
            };
            await dbContext.DeliveryAttempts.AddAsync(attempt, cancellationToken);

            if (success)
            {
                notification.Status = NotificationStatus.Sent;
                notification.SentAt = DateTimeOffset.UtcNow;
                await dbContext.SaveChangesAsync(cancellationToken);
                await queueRepository.CompleteAsync(queueItem.Id, cancellationToken);
                _logger.LogInformation("Notification {NotificationId} sent successfully", notification.Id);
            }
            else
            {
                // Retry with exponential backoff until max attempts, then mark failed
                var attemptCount = queueItem.AttemptCount;
                var backoff = _options.ComputeBackoffSeconds(attemptCount);
                attempt.RetryAfterSeconds = backoff;

                if (attemptCount >= _options.MaxAttempts)
                {
                    notification.Status = NotificationStatus.Failed;
                    await dbContext.SaveChangesAsync(cancellationToken);
                    await queueRepository.MarkFailedAsync(queueItem.Id, cancellationToken); // mark job done as failed terminally
                    _logger.LogWarning("Notification {NotificationId} failed permanently after {Attempts} attempts", notification.Id, attemptCount);
                }
                else
                {
                    notification.Status = NotificationStatus.Pending;
                    await dbContext.SaveChangesAsync(cancellationToken);
                    await queueRepository.FailAsync(queueItem.Id, backoff, cancellationToken);
                    _logger.LogWarning("Notification {NotificationId} failed, retry in {RetryDelay}s (attempt {Attempt})", 
                        notification.Id, backoff, attemptCount);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing notification {NotificationId}", queueItem.NotificationId);
            var attemptCount = queueItem.AttemptCount;
            var backoff = _options.ComputeBackoffSeconds(attemptCount);
            await queueRepository.FailAsync(queueItem.Id, backoff, cancellationToken);
        }
    }

    private async Task RenderTemplate(Notification notification, AppDbContext dbContext, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(notification.TemplateKey)) return;

        var template = await dbContext.Templates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Key == notification.TemplateKey, cancellationToken);

        if (template == null)
        {
            _logger.LogWarning("Template {TemplateKey} not found", notification.TemplateKey);
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering template {TemplateKey}", notification.TemplateKey);
            notification.Subject = template.Subject;
            notification.Body = template.Body;
        }
    }
}

public class QueueWorkerOptions
{
    public int MaxAttempts { get; set; } = 5;
    public int BaseBackoffSeconds { get; set; } = 30;
    public int MaxBackoffSeconds { get; set; } = 3600;
}

internal static class QueueWorkerBackoff
{
    public static int ComputeBackoffSeconds(this QueueWorkerOptions options, int attempt)
    {
        // attempt is 1-based in our code after increment; use attempt as exponent
        var seconds = (int)Math.Min(Math.Pow(2, attempt) * options.BaseBackoffSeconds, options.MaxBackoffSeconds);
        return Math.Max(options.BaseBackoffSeconds, seconds);
    }
}
