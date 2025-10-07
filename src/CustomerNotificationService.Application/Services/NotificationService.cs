using CustomerNotificationService.Application.Interfaces;
using CustomerNotificationService.Application.DTOs;
using CustomerNotificationService.Application.Common;
using CustomerNotificationService.Domain.Entities;
using CustomerNotificationService.Domain.Enums;

namespace CustomerNotificationService.Application.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IQueueRepository _queueRepository;
    private readonly IAuditLogger _auditLogger;

    public NotificationService(INotificationRepository notificationRepository, IQueueRepository queueRepository, IAuditLogger auditLogger)
    {
        _notificationRepository = notificationRepository;
        _queueRepository = queueRepository;
        _auditLogger = auditLogger;
    }

    public async Task<SendNotificationResponse> SendAsync(SendNotificationRequest request, CancellationToken cancellationToken = default)
    {
        // Validate request
        if (string.IsNullOrWhiteSpace(request.Recipient))
            throw new ArgumentException("Recipient is required", nameof(request));
            
        if (string.IsNullOrWhiteSpace(request.TemplateKey) && 
            (string.IsNullOrWhiteSpace(request.Subject) || string.IsNullOrWhiteSpace(request.Body)))
            throw new ArgumentException("Either TemplateKey or both Subject and Body must be provided", nameof(request));

        // Check for idempotency
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existingNotification = await _notificationRepository.GetByIdempotencyKeyAsync(request.IdempotencyKey, cancellationToken);
            if (existingNotification != null)
            {
                // Return existing notification
                return new SendNotificationResponse
                {
                    NotificationId = existingNotification.Id,
                    Status = existingNotification.Status.ToString(),
                    ScheduledAt = existingNotification.SendAt,
                    IdempotencyKey = existingNotification.IdempotencyKey,
                    IsExisting = true
                };
            }
        }

        // Create notification
        var now = DateTimeOffset.UtcNow;
        var initialStatus = (request.SendAt.HasValue && request.SendAt.Value > now)
            ? NotificationStatus.Scheduled
            : NotificationStatus.Pending;

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
            Status = initialStatus,
            IdempotencyKey = request.IdempotencyKey
        };

    // Persist notification
    await _notificationRepository.CreateNotificationAsync(notification, cancellationToken);
    await _auditLogger.LogAsync("NotificationCreated", notification.Id, null);

        // If scheduled for the future, do not enqueue yet
        if (notification.Status == NotificationStatus.Scheduled)
        {
            return new SendNotificationResponse
            {
                NotificationId = notification.Id,
                Status = notification.Status.ToString(),
                ScheduledAt = notification.SendAt,
                IdempotencyKey = notification.IdempotencyKey,
                IsExisting = false
            };
        }
        else
        {
            // Enqueue immediately
            var queueItem = new NotificationQueueItem
            {
                NotificationId = notification.Id,
                ReadyAt = now,
                JobStatus = "Queued",
                AttemptCount = 0
            };
            await _queueRepository.EnqueueAsync(queueItem, cancellationToken);
        }

        return new SendNotificationResponse
        {
            NotificationId = notification.Id,
            Status = notification.Status.ToString(),
            ScheduledAt = notification.SendAt,
            IdempotencyKey = notification.IdempotencyKey,
            IsExisting = false
        };
    }

    public async Task<PagedResult<CustomerNotificationHistoryItemDto>> GetCustomerNotificationHistoryAsync(CustomerNotificationHistoryRequest request, CancellationToken cancellationToken = default)
    {
        // Delegate to repository for optimized EF query
        return await _notificationRepository.GetCustomerNotificationHistoryAsync(request, cancellationToken);
    }
}
