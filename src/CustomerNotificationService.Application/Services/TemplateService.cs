using CustomerNotificationService.Application.Dtos;
using CustomerNotificationService.Domain.Entities;
using CustomerNotificationService.Application.Interfaces;

namespace CustomerNotificationService.Application.Services;

public class TemplateService : ITemplateService
{
    private readonly INotificationTemplateRepository _repo;
    public TemplateService(INotificationTemplateRepository repo) => _repo = repo;

    public async Task<IEnumerable<TemplateDto>> GetAllAsync(CancellationToken ct)
    {
        var items = await _repo.GetAllAsync(ct);
        return items.Select(MapToDto);
    }

    public async Task<TemplateDto?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        return entity == null ? null : MapToDto(entity);
    }

    public async Task<TemplateDto> CreateAsync(TemplateDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Channel) || string.IsNullOrWhiteSpace(dto.Content))
            throw new ArgumentException("Name, Channel and Content are required");

        var entity = new NotificationTemplate
        {
            Id = dto.Id == Guid.Empty ? Guid.NewGuid() : dto.Id,
            Name = dto.Name.Trim(),
            Channel = dto.Channel.Trim(),
            Content = dto.Content,
            CreatedAt = DateTimeOffset.UtcNow
        };
        await _repo.AddAsync(entity, ct);
        return MapToDto(entity);
    }

    public async Task<bool> UpdateAsync(Guid id, TemplateDto dto, CancellationToken ct)
    {
        var existing = await _repo.GetByIdAsync(id, ct);
        if (existing == null) return false;

        if (!string.IsNullOrWhiteSpace(dto.Name)) existing.Name = dto.Name.Trim();
        if (!string.IsNullOrWhiteSpace(dto.Channel)) existing.Channel = dto.Channel.Trim();
        if (!string.IsNullOrWhiteSpace(dto.Content)) existing.Content = dto.Content;
        existing.UpdatedAt = DateTimeOffset.UtcNow;

        await _repo.UpdateAsync(existing, ct);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        var existing = await _repo.GetByIdAsync(id, ct);
        if (existing == null) return false;
        await _repo.DeleteAsync(existing, ct);
        return true;
    }

    private static TemplateDto MapToDto(NotificationTemplate e) => new()
    {
        Id = e.Id,
        Name = e.Name,
        Channel = e.Channel,
        Content = e.Content,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt
    };
}
