using CustomerNotificationService.Application.Dtos;

namespace CustomerNotificationService.Application.Services;

public interface ITemplateService
{
    Task<IEnumerable<TemplateDto>> GetAllAsync(CancellationToken ct);
    Task<TemplateDto?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<TemplateDto> CreateAsync(TemplateDto dto, CancellationToken ct);
    Task<bool> UpdateAsync(Guid id, TemplateDto dto, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}
