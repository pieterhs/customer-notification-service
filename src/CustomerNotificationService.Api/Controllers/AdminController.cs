using CustomerNotificationService.Application.Dtos;
using CustomerNotificationService.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace CustomerNotificationService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly ITemplateService _templateService;

    public AdminController(ITemplateService templateService) => _templateService = templateService;

    [HttpPost("templates/reload")]
    public IActionResult ReloadTemplates()
    {
        // TODO: trigger template reload
        return Ok(new { status = "reloaded" });
    }

    /// <summary>
    /// List all notification templates.
    /// </summary>
    [HttpGet("templates")]
    [ProducesResponseType(typeof(IEnumerable<TemplateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListTemplates(CancellationToken ct)
        => Ok(await _templateService.GetAllAsync(ct));

    /// <summary>
    /// Get a notification template by id.
    /// </summary>
    [HttpGet("templates/{id:guid}")]
    [ProducesResponseType(typeof(TemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTemplate([FromRoute] Guid id, CancellationToken ct)
    {
        var t = await _templateService.GetByIdAsync(id, ct);
        return t == null ? NotFound() : Ok(t);
    }

    /// <summary>
    /// Create a new notification template.
    /// </summary>
    [HttpPost("templates")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTemplate([FromBody] TemplateDto request, CancellationToken ct)
    {
        if (request == null) return BadRequest("Invalid template payload");
        var created = await _templateService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetTemplate), new { id = created.Id }, created.Id);
    }

    /// <summary>
    /// Update an existing notification template.
    /// </summary>
    /// <param name="id">Template id to update.</param>
    /// <param name="request">Fields to update: name, content or channel.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>200 OK if updated, 404 if not found.</returns>
    [HttpPut("templates/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTemplate([FromRoute] Guid id, [FromBody] TemplateDto request, CancellationToken ct)
    {
        var ok = await _templateService.UpdateAsync(id, request, ct);
        return ok ? Ok() : NotFound();
    }

    /// <summary>
    /// Delete a notification template.
    /// </summary>
    /// <param name="id">Template id to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>204 No Content or 404 if not found.</returns>
    [HttpDelete("templates/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTemplate([FromRoute] Guid id, CancellationToken ct)
    {
        var ok = await _templateService.DeleteAsync(id, ct);
        return ok ? NoContent() : NotFound();
    }
}

// Removed local request models in favor of TemplateDto
