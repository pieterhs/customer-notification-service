using CustomerNotificationService.Application.Dtos;
using CustomerNotificationService.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace CustomerNotificationService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Tags("Admin")]
public class AdminController : ControllerBase
{
    private readonly ITemplateService _templateService;

    public AdminController(ITemplateService templateService) => _templateService = templateService;
    
    /// <summary>
    /// Returns all notification templates.
    /// </summary>
    /// <returns>A list of all notification templates in the system.</returns>
    /// <response code="200">Successfully retrieved all templates.</response>
    [HttpGet("templates")]
    [ProducesResponseType(typeof(IEnumerable<TemplateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListTemplates(CancellationToken ct)
        => Ok(await _templateService.GetAllAsync(ct));

    /// <summary>
    /// Returns a single notification template by id.
    /// </summary>
    /// <param name="id">The unique identifier of the template.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The requested notification template.</returns>
    /// <response code="200">Successfully retrieved the template.</response>
    /// <response code="404">Template with the specified id was not found.</response>
    [HttpGet("templates/{id:guid}")]
    [ProducesResponseType(typeof(TemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTemplate([FromRoute] Guid id, CancellationToken ct)
    {
        var t = await _templateService.GetByIdAsync(id, ct);
        return t == null ? NotFound() : Ok(t);
    }

    /// <summary>
    /// Creates a new notification template.
    /// </summary>
    /// <param name="request">The template data including name, channel, and content.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The id of the newly created template.</returns>
    /// <response code="201">Template created successfully.</response>
    /// <response code="400">Invalid template data provided.</response>
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
    /// Updates an existing notification template.
    /// </summary>
    /// <param name="id">The unique identifier of the template to update.</param>
    /// <param name="request">The updated template data including name, channel, and content.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Confirmation of successful update.</returns>
    /// <response code="200">Template updated successfully.</response>
    /// <response code="400">Invalid template data provided.</response>
    /// <response code="404">Template with the specified id was not found.</response>
    [HttpPut("templates/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTemplate([FromRoute] Guid id, [FromBody] TemplateDto request, CancellationToken ct)
    {
        var ok = await _templateService.UpdateAsync(id, request, ct);
        return ok ? Ok() : NotFound();
    }

    /// <summary>
    /// Deletes a notification template.
    /// </summary>
    /// <param name="id">The unique identifier of the template to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Confirmation of successful deletion.</returns>
    /// <response code="204">Template deleted successfully.</response>
    /// <response code="404">Template with the specified id was not found.</response>
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
