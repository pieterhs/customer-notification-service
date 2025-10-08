namespace CustomerNotificationService.Domain.Entities;

public class NotificationTemplate
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Channel { get; set; } = default!;
    public string Content { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}
