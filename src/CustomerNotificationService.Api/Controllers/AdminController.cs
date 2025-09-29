using Microsoft.AspNetCore.Mvc;

namespace CustomerNotificationService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    [HttpPost("templates/reload")]
    public IActionResult ReloadTemplates()
    {
        // TODO: trigger template reload
        return Ok(new { status = "reloaded" });
    }
}
