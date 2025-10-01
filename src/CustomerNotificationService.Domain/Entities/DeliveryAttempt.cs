namespace CustomerNotificationService.Domain.Entities;

public class DeliveryAttempt
{
    public Guid Id { get; set; }
    public Guid NotificationId { get; set; }
    public DateTimeOffset AttemptedAt { get; set; }
    public bool Success { get; set; }
    public string? ResponseMessage { get; set; }
    public string? ErrorMessage { get; set; }
    // Added for richer audit of attempts
    public string? Status { get; set; } // e.g., "Success", "Failed"
    public int? RetryAfterSeconds { get; set; }
}
