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

    [HttpPost("send")]
    public async Task<IActionResult> Send([FromBody] SendNotificationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var notificationId = await _notificationService.SendAsync(request, cancellationToken);
            return Accepted(new { notificationId });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets paginated notification history for a specific customer with optional filters
    /// </summary>
    /// <param name="customerId">The customer ID to get notification history for</param>
    /// <param name="status">Optional status filter (e.g., Pending, Sent, Failed, Scheduled)</param>
    /// <param name="from">Optional start date filter (ISO 8601 format)</param>
    /// <param name="to">Optional end date filter (ISO 8601 format)</param>
    /// <param name="page">Page number (default: 1, minimum: 1)</param>
    /// <param name="pageSize">Page size (default: 20, range: 1-100)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of customer notification history</returns>
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
