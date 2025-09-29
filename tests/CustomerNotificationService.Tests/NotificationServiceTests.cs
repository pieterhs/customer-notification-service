using CustomerNotificationService.Application.Interfaces;
using CustomerNotificationService.Application.Services;
using CustomerNotificationService.Domain.Entities;
using FluentAssertions;
using Moq;

namespace CustomerNotificationService.Tests;

public class NotificationServiceTests
{
    [Fact]
    public async Task EnqueueAsync_ShouldReturn_Id()
    {
        var notification = new Notification { Id = Guid.NewGuid(), Recipient = "test@example.com" };
        var repo = new Mock<INotificationRepository>();
        var queue = new Mock<IQueueService>();
        var service = new NotificationService(repo.Object, queue.Object);

        var id = await service.EnqueueAsync(notification);

        id.Should().NotBeEmpty();
    }
}
