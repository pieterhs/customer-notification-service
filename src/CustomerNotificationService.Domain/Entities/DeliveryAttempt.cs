namespace CustomerNotificationService.Domain.Entities;

public class DeliveryAttempt
{
    public Guid Id { get; set; }
    public Guid NotificationId { get; set; }
    public DateTimeOffset AttemptedAt { get; set; }
    public bool Success { get; set; }
    public string? ResponseMessage { get; set; }
}
