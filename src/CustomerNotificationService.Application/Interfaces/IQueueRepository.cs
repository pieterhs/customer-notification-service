using CustomerNotificationService.Domain.Entities;

namespace CustomerNotificationService.Application.Interfaces;

public interface IQueueRepository
{
    Task EnqueueAsync(NotificationQueueItem item, CancellationToken cancellationToken = default);
    Task<NotificationQueueItem?> DequeueAsync(CancellationToken cancellationToken = default);
    Task CompleteAsync(Guid queueItemId, CancellationToken cancellationToken = default);
    Task FailAsync(Guid queueItemId, int retryAfterSeconds, CancellationToken cancellationToken = default);
    Task<List<NotificationQueueItem>> GetReadyJobsAsync(DateTimeOffset now, CancellationToken cancellationToken = default);
    Task MarkFailedAsync(Guid queueItemId, CancellationToken cancellationToken = default);
}