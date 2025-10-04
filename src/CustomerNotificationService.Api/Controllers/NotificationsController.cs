using CustomerNotificationService.Application.Services;
using CustomerNotificationService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using CustomerNotificationService.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

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

    [HttpGet("{customerId}/history")]
    public async Task<IActionResult> GetHistory(string customerId)
    {
        _logger.LogInformation("Querying notification history for customerId: {CustomerId}", customerId);
        var notifications = await _dbContext.Notifications
            .Where(n => n.CustomerId == customerId)
            .Include(n => n.Id)
            .ToListAsync();

        if (notifications.Count == 0)
        {
            _logger.LogWarning("No notifications found for customerId: {CustomerId}", customerId);
            return NotFound();
        }

        var notificationIds = notifications.Select(n => n.Id).ToList();
        var attempts = await _dbContext.DeliveryAttempts
            .Where(a => notificationIds.Contains(a.NotificationId))
            .ToListAsync();

        var tz = TimeZoneInfo.FindSystemTimeZoneById("South Africa Standard Time");
        var result = notifications.Select(n => new Application.Dtos.NotificationHistoryDto
        {
            NotificationId = n.Id,
            TemplateKey = n.TemplateKey,
            Subject = n.Subject,
            Status = n.Status.ToString(),            
            SendAt = n.SendAt.HasValue
                ? TimeZoneInfo.ConvertTime(n.SendAt.Value, tz)
                : (DateTimeOffset?)null,
            Attempts = attempts
                .Where(a => a.NotificationId == n.Id)
                .OrderBy(a => a.AttemptedAt)
                .Select(a => new Application.Dtos.DeliveryAttemptDto
                {
                    AttemptedAt = TimeZoneInfo.ConvertTime(a.AttemptedAt, tz),
                    Status = a.Status ?? (a.Success ? "Success" : "Failed"),
                    ErrorMessage = a.ErrorMessage
                })
                .ToList()
        }).ToList();

    _logger.LogInformation("Returning {Count} notifications for customerId: {CustomerId}", result.Count, customerId);
    return Ok(result);
    }
}
