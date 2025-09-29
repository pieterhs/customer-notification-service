using CustomerNotificationService.Domain.Entities;

namespace CustomerNotificationService.Application.Interfaces;

public interface IQueueRepository
{
    Task EnqueueAsync(NotificationQueueItem item, CancellationToken cancellationToken = default);
    Task<NotificationQueueItem?> DequeueAsync(CancellationToken cancellationToken = default);
    Task CompleteAsync(Guid queueItemId, CancellationToken cancellationToken = default);
    Task FailAsync(Guid queueItemId, int retryDelayMinutes, CancellationToken cancellationToken = default);
}