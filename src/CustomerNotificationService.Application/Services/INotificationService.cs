namespace CustomerNotificationService.Application.Services;

using CustomerNotificationService.Domain.Entities;

public interface INotificationService
{
    Task<Guid> EnqueueAsync(Notification notification, CancellationToken cancellationToken = default);
}
