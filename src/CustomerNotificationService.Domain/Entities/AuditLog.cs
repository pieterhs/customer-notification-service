namespace CustomerNotificationService.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string PerformedBy { get; set; } = string.Empty;
    public DateTimeOffset PerformedAt { get; set; }
    public string? Details { get; set; }
}
