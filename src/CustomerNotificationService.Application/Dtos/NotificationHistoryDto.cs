namespace CustomerNotificationService.Application.Dtos;

using System;
using System.Collections.Generic;

public class NotificationHistoryDto
{
    public Guid NotificationId { get; set; }
    public string? TemplateKey { get; set; }
    public string? Subject { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset? SendAt { get; set; }
    public List<DeliveryAttemptDto> Attempts { get; set; } = new();
}

public class DeliveryAttemptDto
{
    public DateTimeOffset Timestamp { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}
