using CustomerNotificationService.Domain.Entities;

namespace CustomerNotificationService.Application.Interfaces;

public interface INotificationTemplateRepository
{
    Task<List<NotificationTemplate>> GetAllAsync(CancellationToken ct);
    Task<NotificationTemplate?> GetByIdAsync(Guid id, CancellationToken ct);
    Task AddAsync(NotificationTemplate template, CancellationToken ct);
    Task UpdateAsync(NotificationTemplate template, CancellationToken ct);
    Task DeleteAsync(NotificationTemplate template, CancellationToken ct);
}
