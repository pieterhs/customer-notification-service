using CustomerNotificationService.Application.Interfaces;
using CustomerNotificationService.Domain.Entities;
using CustomerNotificationService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CustomerNotificationService.Infrastructure.Repositories;

public class QueueRepository : IQueueRepository
{
    private readonly AppDbContext _db;
    
    public QueueRepository(AppDbContext db) => _db = db;

    public async Task EnqueueAsync(NotificationQueueItem item, CancellationToken cancellationToken = default)
    {
        item.Id = Guid.NewGuid();
        item.EnqueuedAt = DateTimeOffset.UtcNow;
        item.JobStatus = "Pending";
        
        await _db.NotificationQueue.AddAsync(item, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<NotificationQueueItem?> DequeueAsync(CancellationToken cancellationToken = default)
    {
        // Try to use transaction if supported, otherwise fall back to simple approach
        try
        {
            using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
            
            var item = await _db.NotificationQueue
                .Where(q => q.JobStatus == "Pending" && q.ReadyAt <= DateTimeOffset.UtcNow)
                .OrderBy(q => q.ReadyAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (item != null)
            {
                item.JobStatus = "Processing";
                item.AttemptCount++;
                await _db.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return item;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Transactions are not supported"))
        {
            // Fallback for in-memory database
            var item = await _db.NotificationQueue
                .Where(q => q.JobStatus == "Pending" && q.ReadyAt <= DateTimeOffset.UtcNow)
                .OrderBy(q => q.ReadyAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (item != null)
            {
                item.JobStatus = "Processing";
                item.AttemptCount++;
                await _db.SaveChangesAsync(cancellationToken);
            }

            return item;
        }
    }

    public async Task CompleteAsync(Guid queueItemId, CancellationToken cancellationToken = default)
    {
        var item = await _db.NotificationQueue.FindAsync([queueItemId], cancellationToken);
        if (item != null)
        {
            item.JobStatus = "Completed";
            item.CompletedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task FailAsync(Guid queueItemId, int retryDelayMinutes, CancellationToken cancellationToken = default)
    {
        var item = await _db.NotificationQueue.FindAsync([queueItemId], cancellationToken);
        if (item != null)
        {
            if (item.AttemptCount >= 3)
            {
                item.JobStatus = "Failed";
                item.CompletedAt = DateTimeOffset.UtcNow;
            }
            else
            {
                item.JobStatus = "Pending";
                item.ReadyAt = DateTimeOffset.UtcNow.AddMinutes(retryDelayMinutes);
            }
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}