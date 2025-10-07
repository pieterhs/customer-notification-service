using CustomerNotificationService.Application.Services;
using CustomerNotificationService.Application.DTOs;
using CustomerNotificationService.Application.Common;
using CustomerNotificationService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using CustomerNotificationService.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace CustomerNotificationService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(INotificationService notificationService, AppDbContext dbContext, ILogger<NotificationsController> logger)
    {
        _notificationService = notificationService;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Sends a notification to a recipient via the specified channel
    /// </summary>
    /// <param name="request">The notification request containing recipient, template, and channel information</param>
    /// <param name="idempotencyKey">Optional idempotency key to prevent duplicate notifications</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The notification response with ID, status, and metadata</returns>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/notifications/send
    ///     Content-Type: application/json
    ///     X-Api-Key: dev-api-key-12345
    ///     Idempotency-Key: order-12345
    ///     
    ///     {
    ///         "customerId": "11111111-2222-3333-4444-555555555555",
    ///         "recipient": "user@example.com",
    ///         "templateKey": "welcome",
    ///         "channel": 0,
    ///         "payloadJson": "{\"name\": \"John Doe\"}",
    ///         "sendAt": "2025-10-07T18:00:00Z"
    ///     }
    ///     
    /// Sample response (202 Accepted for new notification):
    /// 
    ///     {
    ///         "notificationId": "12345678-1234-1234-1234-123456789abc",
    ///         "status": "Scheduled",
    ///         "scheduledAt": "2025-10-07T18:00:00Z",
    ///         "idempotencyKey": "order-12345",
    ///         "isExisting": false
    ///     }
    /// </remarks>
    [HttpPost("send")]
    [ProducesResponseType(typeof(SendNotificationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SendNotificationResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Send(
        [FromBody] SendNotificationRequest request, 
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Create request with idempotency key from header
            var requestWithIdempotency = request with { IdempotencyKey = idempotencyKey };
            
            var response = await _notificationService.SendAsync(requestWithIdempotency, cancellationToken);
            
            // Return 200 OK for existing notifications (idempotent), 202 Accepted for new ones
            return response.IsExisting ? Ok(response) : Accepted(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets paginated notification history for a specific customer with optional filters
    /// </summary>
    /// <param name="customerId">The customer ID (GUID format) to get notification history for</param>
    /// <param name="status">Optional status filter (e.g., Pending, Sent, Failed, Scheduled)</param>
    /// <param name="from">Optional start date filter (ISO 8601 format)</param>
    /// <param name="to">Optional end date filter (ISO 8601 format)</param>
    /// <param name="page">Page number (default: 1, minimum: 1)</param>
    /// <param name="pageSize">Page size (default: 20, range: 1-100)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of customer notification history</returns>
    /// <remarks>
    /// Sample request:
    /// 
    ///     GET /api/notifications/customer/11111111-2222-3333-4444-555555555555/history?status=Sent&amp;page=1&amp;pageSize=20
    ///     X-Api-Key: dev-api-key-12345
    ///     
    /// Sample response:
    /// 
    ///     {
    ///         "items": [
    ///             {
    ///                 "notificationId": "12345678-1234-1234-1234-123456789abc",
    ///                 "customerId": "11111111-2222-3333-4444-555555555555",
    ///                 "templateId": "welcome",
    ///                 "channel": "Email",
    ///                 "status": "Sent",
    ///                 "attemptCount": 1,
    ///                 "lastError": null,
    ///                 "createdAt": "2025-10-07T19:55:29.120748+00:00",
    ///                 "scheduledAt": null,
    ///                 "sentAt": "2025-10-07T19:55:30.866886+00:00",
    ///                 "failedAt": null,
    ///                 "renderedPreview": "Welcome John Doe!"
    ///             }
    ///         ],
    ///         "page": 1,
    ///         "pageSize": 20,
    ///         "totalItems": 1,
    ///         "totalPages": 1,
    ///         "hasNext": false,
    ///         "hasPrevious": false
    ///     }
    /// </remarks>
    /// <response code="200">Returns the paginated notification history</response>
    /// <response code="400">Invalid query parameters</response>
    /// <response code="404">Customer not found or no notifications found</response>
    [HttpGet("{customerId}/history")]
    [ProducesResponseType(typeof(PagedResult<CustomerNotificationHistoryItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHistory(
        [FromRoute] string customerId,
        [FromQuery] string? status = null,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        // Validate input parameters
        if (string.IsNullOrWhiteSpace(customerId))
        {
            return BadRequest(new { error = "Customer ID is required" });
        }

        if (!Guid.TryParse(customerId, out var customerGuid))
        {
            return BadRequest(new { error = "Customer ID must be a valid GUID" });
        }

        if (page < 1)
        {
            return BadRequest(new { error = "Page must be greater than or equal to 1" });
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return BadRequest(new { error = "Page size must be between 1 and 100" });
        }

        if (from.HasValue && to.HasValue && from.Value > to.Value)
        {
            return BadRequest(new { error = "From date cannot be greater than to date" });
        }

        try
        {
            _logger.LogInformation("Querying notification history for customerId: {CustomerId}, page: {Page}, pageSize: {PageSize}, status: {Status}", 
                customerId, page, pageSize, status);

            var request = new CustomerNotificationHistoryRequest
            {
                CustomerId = customerGuid,
                Status = status,
                From = from,
                To = to,
                Page = page,
                PageSize = pageSize
            };

            var result = await _notificationService.GetCustomerNotificationHistoryAsync(request, cancellationToken);

            if (result.TotalItems == 0)
            {
                _logger.LogWarning("No notifications found for customerId: {CustomerId}", customerId);
                return NotFound(new { message = "No notifications found for the specified customer" });
            }

            _logger.LogInformation("Returning {Count} notifications (page {Page} of {TotalPages}) for customerId: {CustomerId}", 
                result.Items.Count, result.Page, result.TotalPages, customerId);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid request parameters: {Error}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notification history for customerId: {CustomerId}", customerId);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { error = "An error occurred while retrieving notification history" });
        }
    }
}
