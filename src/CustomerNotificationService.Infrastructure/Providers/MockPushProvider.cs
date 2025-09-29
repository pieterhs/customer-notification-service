using CustomerNotificationService.Domain.Entities;

namespace CustomerNotificationService.Infrastructure.Providers;

public class MockPushProvider : INotificationProvider
{
    public string Channel => "Push";

    public Task<bool> SendAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        // TODO: simulate push notification sending
        return Task.FromResult(true);
    }
}
