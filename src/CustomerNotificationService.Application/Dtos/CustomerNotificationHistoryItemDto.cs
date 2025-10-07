using System;

namespace CustomerNotificationService.Application.DTOs;

/// <summary>
/// Represents a customer notification history item with detailed information
/// </summary>
public class CustomerNotificationHistoryItemDto
{
    /// <summary>
    /// Unique identifier of the notification
    /// </summary>
    public Guid NotificationId { get; set; }

    /// <summary>
    /// Unique identifier of the customer
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Template identifier used for this notification
    /// </summary>
    public string TemplateId { get; set; } = string.Empty;

    /// <summary>
    /// Communication channel (Email, SMS, Push, etc.)
    /// </summary>
    public string Channel { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the notification
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Number of delivery attempts made
    /// </summary>
    public int AttemptCount { get; set; }

    /// <summary>
    /// Last error message if the notification failed
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    /// When the notification was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// When the notification is scheduled to be sent (if scheduled)
    /// </summary>
    public DateTimeOffset? ScheduledAt { get; set; }

    /// <summary>
    /// When the notification was successfully sent
    /// </summary>
    public DateTimeOffset? SentAt { get; set; }

    /// <summary>
    /// When the notification failed permanently
    /// </summary>
    public DateTimeOffset? FailedAt { get; set; }

    /// <summary>
    /// Preview of the rendered notification content
    /// </summary>
    public string? RenderedPreview { get; set; }
}