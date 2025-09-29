using CustomerNotificationService.Domain.Entities;

namespace CustomerNotificationService.Infrastructure.Providers;

public class MockEmailProvider : INotificationProvider
{
    public string Channel => "Email";

    public Task<bool> SendAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        // TODO: simulate email sending
        return Task.FromResult(true);
    }
}
