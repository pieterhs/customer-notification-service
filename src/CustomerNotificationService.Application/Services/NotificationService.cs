using CustomerNotificationService.Application.Interfaces;
using CustomerNotificationService.Application.Dtos;
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

    public async Task<Guid> SendAsync(SendNotificationRequest request, CancellationToken cancellationToken = default)
    {
        // Validate request
        if (string.IsNullOrWhiteSpace(request.Recipient))
            throw new ArgumentException("Recipient is required", nameof(request));
            
        if (string.IsNullOrWhiteSpace(request.TemplateKey) && 
            (string.IsNullOrWhiteSpace(request.Subject) || string.IsNullOrWhiteSpace(request.Body)))
            throw new ArgumentException("Either TemplateKey or both Subject and Body must be provided", nameof(request));

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
            Status = initialStatus
        };

    // Persist notification
    await _notificationRepository.CreateNotificationAsync(notification, cancellationToken);
    await _auditLogger.LogAsync("NotificationCreated", notification.Id, null);

        // If scheduled for the future, do not enqueue yet
        if (notification.Status == NotificationStatus.Scheduled)
        {
            return notification.Id;
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

        return notification.Id;
    }

    public async Task<PagedResult<CustomerNotificationHistoryItemDto>> GetCustomerNotificationHistoryAsync(CustomerNotificationHistoryRequest request, CancellationToken cancellationToken = default)
    {
        // Get notifications with filters applied
        var query = await _notificationRepository.GetNotificationsByCustomerIdAsync(request.CustomerId, cancellationToken);
        
        // Apply status filter if provided
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (Enum.TryParse<NotificationStatus>(request.Status, true, out var statusEnum))
            {
                query = query.Where(n => n.Status == statusEnum);
            }
            else
            {
                throw new ArgumentException($"Invalid status value: {request.Status}");
            }
        }

        // Apply date range filters
        if (request.From.HasValue)
        {
            query = query.Where(n => n.CreatedAt >= request.From.Value);
        }

        if (request.To.HasValue)
        {
            query = query.Where(n => n.CreatedAt <= request.To.Value);
        }

        // Get total count for pagination
        var totalItems = query.Count();
        var totalPages = (int)Math.Ceiling((double)totalItems / request.PageSize);

        // Apply pagination
        var notifications = query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        // Get delivery attempts for the notifications
        var notificationIds = notifications.Select(n => n.Id).ToList();
        var attempts = await _notificationRepository.GetDeliveryAttemptsByNotificationIdsAsync(notificationIds, cancellationToken);

        // Map to DTOs
        var items = notifications.Select(n => new CustomerNotificationHistoryItemDto
        {
            NotificationId = n.Id,
            TemplateKey = n.TemplateKey,
            Subject = n.Subject,
            Status = n.Status.ToString(),
            SendAt = n.SendAt,
            CreatedAt = n.CreatedAt,
            Channel = n.Channel.ToString(),
            Attempts = attempts
                .Where(a => a.NotificationId == n.Id)
                .OrderBy(a => a.AttemptedAt)
                .Select(a => new DeliveryAttemptDto
                {
                    AttemptedAt = a.AttemptedAt,
                    Status = a.Status ?? (a.Success ? "Success" : "Failed"),
                    ErrorMessage = a.ErrorMessage
                })
                .ToList()
        }).ToList();

        return new PagedResult<CustomerNotificationHistoryItemDto>
        {
            Items = items,
            TotalItems = totalItems,
            TotalPages = totalPages,
            CurrentPage = request.Page,
            PageSize = request.PageSize,
            HasNext = request.Page < totalPages,
            HasPrevious = request.Page > 1
        };
    }
}
