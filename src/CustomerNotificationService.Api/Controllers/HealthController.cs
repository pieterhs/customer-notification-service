using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CustomerNotificationService.Api.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly Infrastructure.Data.AppDbContext _dbContext;

    public HealthController(Infrastructure.Data.AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public IActionResult Get() => Ok(new { status = "ok", time = DateTimeOffset.UtcNow });

    [HttpGet("/metrics")]
    public async Task<IActionResult> GetMetrics()
    {
        var now = DateTimeOffset.UtcNow;
        var oneHourAgo = now.AddHours(-1);

        var queueDepth = await _dbContext.NotificationQueue
            .CountAsync(q => q.JobStatus == "Queued");

        var pendingNotifications = await _dbContext.Notifications
            .CountAsync(n => n.Status == Domain.Enums.NotificationStatus.Pending);

        var failedNotificationsLastHour = await _dbContext.DeliveryAttempts
            .CountAsync(a => a.Status == "Failed" && a.AttemptedAt >= oneHourAgo);

        var metrics = new
        {
            queueDepth,
            pendingNotifications,
            failedNotificationsLastHour
        };
        return Ok(metrics);
    }
}
