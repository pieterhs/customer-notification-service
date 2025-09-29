using CustomerNotificationService.Domain.Enums;

namespace CustomerNotificationService.Domain.Entities;

public class Notification
{
    public Guid Id { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string? Body { get; set; }
    public string? TemplateKey { get; set; }
    public ChannelType Channel { get; set; }
    public NotificationStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ScheduledAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
}
