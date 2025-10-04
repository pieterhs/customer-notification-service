namespace CustomerNotificationService.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; } // UTC
    public string Action { get; set; } = string.Empty;
    public Guid? NotificationId { get; set; }
    public string? Details { get; set; }
}
