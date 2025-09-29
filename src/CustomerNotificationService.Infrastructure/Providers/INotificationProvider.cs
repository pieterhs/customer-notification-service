using CustomerNotificationService.Domain.Entities;

namespace CustomerNotificationService.Infrastructure.Providers;

public interface INotificationProvider
{
    string Channel { get; }
    Task<bool> SendAsync(Notification notification, CancellationToken cancellationToken = default);
}
