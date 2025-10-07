namespace CustomerNotificationService.Application.Dtos;

using System;
using System.Collections.Generic;

public class CustomerNotificationHistoryRequest
{
    public string CustomerId { get; set; } = string.Empty;
    public string? Status { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public bool HasNext { get; set; }
    public bool HasPrevious { get; set; }
}

public class CustomerNotificationHistoryItemDto
{
    public Guid NotificationId { get; set; }
    public string? TemplateKey { get; set; }
    public string? Subject { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset? SendAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string Channel { get; set; } = string.Empty;
    public List<DeliveryAttemptDto> Attempts { get; set; } = new();
}