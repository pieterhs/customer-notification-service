namespace CustomerNotificationService.Application.DTOs;

/// <summary>
/// Response model for send notification operations
/// </summary>
public class SendNotificationResponse
{
    /// <summary>
    /// The unique identifier of the notification
    /// </summary>
    public Guid NotificationId { get; set; }

    /// <summary>
    /// The current status of the notification
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// When the notification is scheduled to be sent (if applicable)
    /// </summary>
    public DateTimeOffset? ScheduledAt { get; set; }

    /// <summary>
    /// The idempotency key used for this request (if provided)
    /// </summary>
    public string? IdempotencyKey { get; set; }

    /// <summary>
    /// Indicates whether this response is for an existing notification (idempotent request)
    /// </summary>
    public bool IsExisting { get; set; }
}