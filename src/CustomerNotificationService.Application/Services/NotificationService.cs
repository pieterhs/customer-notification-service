using CustomerNotificationService.Application.Interfaces;
using CustomerNotificationService.Domain.Entities;
using CustomerNotificationService.Domain.Enums;

namespace CustomerNotificationService.Application.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IQueueRepository _queueRepository;

    public NotificationService(INotificationRepository notificationRepository, IQueueRepository queueRepository)
    {
        _notificationRepository = notificationRepository;
        _queueRepository = queueRepository;
    }

    public async Task<Guid> SendAsync(SendNotificationRequest request, CancellationToken cancellationToken = default)
    {
        // Validate request
        if (string.IsNullOrWhiteSpace(request.Recipient))
            throw new ArgumentException("Recipient is required", nameof(request));
            
        if (string.IsNullOrWhiteSpace(request.TemplateKey) && 
            (string.IsNullOrWhiteSpace(request.Subject) || string.IsNullOrWhiteSpace(request.Body)))
            throw new ArgumentException("Either TemplateKey or both Subject and Body must be provided", nameof(request));

        // Create notification
        var notification = new Notification
        {
            Recipient = request.Recipient,
            TemplateKey = request.TemplateKey,
            Subject = request.Subject,
            Body = request.Body,
            PayloadJson = request.PayloadJson,
            Channel = request.Channel,
            SendAt = request.SendAt,
            CustomerId = request.CustomerId,
            Status = NotificationStatus.Pending
        };

        // Persist notification
        await _notificationRepository.CreateNotificationAsync(notification, cancellationToken);

        // Enqueue job
        var queueItem = new NotificationQueueItem
        {
            NotificationId = notification.Id,
            ReadyAt = request.SendAt ?? DateTimeOffset.UtcNow
        };

        await _queueRepository.EnqueueAsync(queueItem, cancellationToken);

        return notification.Id;
    }
}
