using CustomerNotificationService.Domain.Entities;

namespace CustomerNotificationService.Application.Interfaces;

public interface IQueueService
{
    Task EnqueueAsync(NotificationQueueItem item, CancellationToken cancellationToken = default);
    Task<NotificationQueueItem?> DequeueAsync(CancellationToken cancellationToken = default);
}
