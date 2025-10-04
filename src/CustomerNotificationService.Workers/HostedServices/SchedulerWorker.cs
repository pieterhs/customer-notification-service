using CustomerNotificationService.Domain.Entities;
using CustomerNotificationService.Domain.Enums;
using CustomerNotificationService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CustomerNotificationService.Workers.HostedServices;

public class SchedulerWorker : BackgroundService
{
    private readonly ILogger<SchedulerWorker> _logger;
    private readonly IServiceProvider _services;

    public SchedulerWorker(ILogger<SchedulerWorker> logger, IServiceProvider services)
    {
        _logger = logger;
        _services = services;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SchedulerWorker started - running every 30 seconds");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessScheduledNotificationsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing scheduled notifications");
            }
            
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    private async Task ProcessScheduledNotificationsAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var auditLogger = scope.ServiceProvider.GetRequiredService<CustomerNotificationService.Application.Interfaces.IAuditLogger>();
        
        var now = DateTimeOffset.UtcNow;
        
        try
        {
            // Use transaction for consistency
            using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            
            // Find notifications that are scheduled and due, but not already enqueued
            var eligibleNotifications = await dbContext.Notifications
                .Where(n => n.Status == NotificationStatus.Scheduled && 
                           n.SendAt != null && 
                           n.SendAt <= now &&
                           !dbContext.NotificationQueue.Any(q => q.NotificationId == n.Id))
                .ToListAsync(cancellationToken);

            if (eligibleNotifications.Count == 0)
            {
                _logger.LogDebug("No eligible scheduled notifications found for promotion at {ProcessTime}", now);
                await transaction.CommitAsync(cancellationToken);
                return;
            }

            _logger.LogInformation("Found {Count} eligible scheduled notifications for promotion", eligibleNotifications.Count);

            var promotedNotificationIds = new List<Guid>();

            foreach (var notification in eligibleNotifications)
            {
                try
                {
                    // Create queue item
                    var queueItem = new NotificationQueueItem
                    {
                        Id = Guid.NewGuid(),
                        NotificationId = notification.Id,
                        EnqueuedAt = now,
                        ReadyAt = now,
                        JobStatus = "Queued",
                        AttemptCount = 0,
                        NextAttemptAt = null
                    };

                    await dbContext.NotificationQueue.AddAsync(queueItem, cancellationToken);

                    // Update notification status
                    notification.Status = NotificationStatus.Pending;

                    promotedNotificationIds.Add(notification.Id);

                    await auditLogger.LogAsync("NotificationEnqueued", notification.Id, null);

                    _logger.LogDebug("Promoted scheduled notification {NotificationId} to queue (SendAt: {SendAt})", 
                        notification.Id, notification.SendAt);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error promoting scheduled notification {NotificationId}", notification.Id);
                    // Continue with other notifications rather than failing the entire batch
                }
            }

            // Save all changes in a single transaction
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            
            if (promotedNotificationIds.Count > 0)
            {
                _logger.LogInformation("Successfully promoted {Count} scheduled notifications to queue: [{NotificationIds}]", 
                    promotedNotificationIds.Count, string.Join(", ", promotedNotificationIds));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing scheduled notifications batch at {ProcessTime}", now);
            // Transaction will be automatically rolled back
            throw;
        }
    }
}
