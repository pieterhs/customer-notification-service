using CustomerNotificationService.Domain.Entities;

namespace CustomerNotificationService.Application.Interfaces;

public interface ITemplateRepository
{
    Task<Template?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
}
