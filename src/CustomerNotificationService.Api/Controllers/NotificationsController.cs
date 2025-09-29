using CustomerNotificationService.Application.Services;
using CustomerNotificationService.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace CustomerNotificationService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
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

    [HttpGet("{id}")]
    public IActionResult GetNotification(Guid id)
    {
        // TODO: get from repository
        return Ok(new { id });
    }
}
