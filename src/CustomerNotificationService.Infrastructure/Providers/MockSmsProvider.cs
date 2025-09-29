using CustomerNotificationService.Domain.Entities;

namespace CustomerNotificationService.Infrastructure.Providers;

public class MockSmsProvider : INotificationProvider
{
    public string Channel => "Sms";

    public Task<bool> SendAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        // TODO: simulate SMS sending
        return Task.FromResult(true);
    }
}
