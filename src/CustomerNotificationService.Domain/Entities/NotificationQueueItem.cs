namespace CustomerNotificationService.Domain.Entities;

public class NotificationQueueItem
{
    public Guid Id { get; set; }
    public Guid NotificationId { get; set; }
    public DateTimeOffset EnqueuedAt { get; set; }
    public DateTimeOffset? AvailableAfter { get; set; }
}
