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
        _logger.LogInformation("SchedulerWorker started");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await MoveScheduledToQueueAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving scheduled notifications to queue");
            }
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    private async Task MoveScheduledToQueueAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        var toQueue = await db.Notifications
            .Where(n => n.Status == NotificationStatus.Scheduled && n.SendAt != null && n.SendAt <= now)
            .ToListAsync(ct);

        if (toQueue.Count == 0) return;

        foreach (var n in toQueue)
        {
            n.Status = NotificationStatus.Pending; // now pending processing
            db.NotificationQueue.Add(new Domain.Entities.NotificationQueueItem
            {
                Id = Guid.NewGuid(),
                NotificationId = n.Id,
                EnqueuedAt = now,
                ReadyAt = now,
                JobStatus = "Queued",
                AttemptCount = 0
            });
        }

        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Scheduler enqueued {Count} notifications", toQueue.Count);
    }
}
