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
        
        var now = DateTimeOffset.UtcNow;
        
        // Find notifications that are scheduled and due
        var dueNotifications = await dbContext.Notifications
            .Where(n => n.Status == NotificationStatus.Scheduled && 
                       n.SendAt != null && 
                       n.SendAt <= now)
            .ToListAsync(cancellationToken);

        if (dueNotifications.Count == 0)
        {
            _logger.LogDebug("No scheduled notifications due for processing");
            return;
        }

        _logger.LogInformation("Found {Count} scheduled notifications due for processing", dueNotifications.Count);

        foreach (var notification in dueNotifications)
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

                dbContext.NotificationQueue.Add(queueItem);
                
                // Update notification status
                notification.Status = NotificationStatus.Pending;
                
                _logger.LogDebug("Enqueued scheduled notification {NotificationId} for processing", notification.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enqueuing scheduled notification {NotificationId}", notification.Id);
            }
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Successfully enqueued {Count} scheduled notifications", dueNotifications.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving scheduled notification changes to database");
        }
    }
}
