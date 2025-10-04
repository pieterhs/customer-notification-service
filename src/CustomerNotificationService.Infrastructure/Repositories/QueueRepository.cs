using CustomerNotificationService.Application.Interfaces;
using CustomerNotificationService.Domain.Entities;
using CustomerNotificationService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace CustomerNotificationService.Infrastructure.Repositories;

public class QueueRepository : IQueueRepository
{
    private readonly AppDbContext _db;
    
    public QueueRepository(AppDbContext db) => _db = db;

    public async Task EnqueueAsync(NotificationQueueItem item, CancellationToken cancellationToken = default)
    {
        item.Id = Guid.NewGuid();
        item.EnqueuedAt = DateTimeOffset.UtcNow;
        item.JobStatus = "Queued";
        
        await _db.NotificationQueue.AddAsync(item, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<NotificationQueueItem?> DequeueAsync(CancellationToken cancellationToken = default)
    {
        // Try to use transaction if supported, otherwise fall back to simple approach
        try
        {
            using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
            
            var now = DateTimeOffset.UtcNow;
            NotificationQueueItem? item;
            if (_db.Database.IsNpgsql())
            {
                // Use explicit SQL with FOR UPDATE SKIP LOCKED for safe concurrent dequeues
                        item = await _db.NotificationQueue
                            .FromSqlInterpolated($@"SELECT * FROM ""NotificationQueue"" WHERE ""JobStatus"" = 'Queued' AND ""ReadyAt"" <= {now} AND (""NextAttemptAt"" IS NULL OR ""NextAttemptAt"" <= {now}) ORDER BY ""ReadyAt"" LIMIT 1 FOR UPDATE SKIP LOCKED")
                    .AsTracking()
                    .FirstOrDefaultAsync(cancellationToken);
            }
            else
            {
                item = await _db.NotificationQueue
                    .Where(q => q.JobStatus == "Queued" && q.ReadyAt <= now && (q.NextAttemptAt == null || q.NextAttemptAt <= now))
                    .OrderBy(q => q.ReadyAt)
                    .FirstOrDefaultAsync(cancellationToken);
            }

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
            var now = DateTimeOffset.UtcNow;
            var item = await _db.NotificationQueue
                .Where(q => q.JobStatus == "Queued" && q.ReadyAt <= now && (q.NextAttemptAt == null || q.NextAttemptAt <= now))
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

    public async Task FailAsync(Guid queueItemId, int retryAfterSeconds, CancellationToken cancellationToken = default)
    {
        var item = await _db.NotificationQueue.FindAsync([queueItemId], cancellationToken);
        if (item != null)
        {
            item.JobStatus = "Queued";
            item.NextAttemptAt = DateTimeOffset.UtcNow.AddSeconds(retryAfterSeconds);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<List<NotificationQueueItem>> GetReadyJobsAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        return await _db.NotificationQueue
            .Where(q => q.JobStatus == "Queued" && (q.NextAttemptAt == null || q.NextAttemptAt <= now))
            .OrderBy(q => q.ReadyAt)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkFailedAsync(Guid queueItemId, CancellationToken cancellationToken = default)
    {
        var item = await _db.NotificationQueue.FindAsync([queueItemId], cancellationToken);
        if (item != null)
        {
            item.JobStatus = "Failed";
            item.CompletedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}