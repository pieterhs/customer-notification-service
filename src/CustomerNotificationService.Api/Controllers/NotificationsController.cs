using CustomerNotificationService.Application.Services;
using CustomerNotificationService.Domain.Entities;
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

    [HttpPost]
    public IActionResult CreateNotification([FromBody] Notification notification)
    {
        // TODO: call service to enqueue notification
        return Accepted(notification);
    }

    [HttpGet("{id}")]
    public IActionResult GetNotification(Guid id)
    {
        // TODO: get from repository
        return Ok(new { id });
    }
}
