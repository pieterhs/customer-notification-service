using CustomerNotificationService.Application.Interfaces;
using CustomerNotificationService.Domain.Entities;

namespace CustomerNotificationService.Application.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IQueueService _queueService;

    public NotificationService(INotificationRepository notificationRepository, IQueueService queueService)
    {
        _notificationRepository = notificationRepository;
        _queueService = queueService;
    }

    public async Task<Guid> EnqueueAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        // TODO: persist notification and enqueue
        await Task.CompletedTask;
        return Guid.NewGuid();
    }
}
