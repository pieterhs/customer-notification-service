namespace CustomerNotificationService.Domain.Entities;

public class NotificationQueueItem
{
    public Guid Id { get; set; }
    public Guid NotificationId { get; set; }
    public DateTimeOffset EnqueuedAt { get; set; }
    public DateTimeOffset ReadyAt { get; set; }
    public string JobStatus { get; set; } = "Queued";
    public int AttemptCount { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? NextAttemptAt { get; set; }
}
