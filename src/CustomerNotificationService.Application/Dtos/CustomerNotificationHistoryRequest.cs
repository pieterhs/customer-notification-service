using System;
using System.ComponentModel.DataAnnotations;

namespace CustomerNotificationService.Application.DTOs;

/// <summary>
/// Request model for retrieving customer notification history with filters and pagination
/// </summary>
public class CustomerNotificationHistoryRequest
{
    /// <summary>
    /// The unique identifier of the customer
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Optional status filter (e.g., Pending, Sent, Failed, Scheduled)
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Optional start date filter for created notifications
    /// </summary>
    public DateTimeOffset? From { get; set; }

    /// <summary>
    /// Optional end date filter for created notifications
    /// </summary>
    public DateTimeOffset? To { get; set; }

    /// <summary>
    /// Page number (1-based indexing)
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than or equal to 1")]
    public int Page { get; set; } = 1;

    /// <summary>
    /// Number of items per page
    /// </summary>
    [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100")]
    public int PageSize { get; set; } = 20;
}