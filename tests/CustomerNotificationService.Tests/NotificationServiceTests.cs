using CustomerNotificationService.Application.Interfaces;
using CustomerNotificationService.Application.Services;
using CustomerNotificationService.Domain.Entities;
using CustomerNotificationService.Domain.Enums;
using CustomerNotificationService.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace CustomerNotificationService.Tests;

public class NotificationServiceTests
{
    [Fact]
    public async Task SendAsync_ShouldReturn_NotificationId()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);
        var notificationRepo = new CustomerNotificationService.Infrastructure.Repositories.NotificationRepository(context);
        var queueRepo = new Mock<IQueueRepository>();
        
        var service = new NotificationService(notificationRepo, queueRepo.Object);

        var request = new SendNotificationRequest(
            Recipient: "test@example.com",
            TemplateKey: null,
            Subject: "Test Subject",
            Body: "Test Body",
            PayloadJson: null,
            Channel: ChannelType.Email
        );

        // Act
        var id = await service.SendAsync(request);

        // Assert
        id.Should().NotBeEmpty();
        queueRepo.Verify(q => q.EnqueueAsync(It.IsAny<NotificationQueueItem>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_Should_EnqueueJob()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);
        var notificationRepo = new CustomerNotificationService.Infrastructure.Repositories.NotificationRepository(context);
        var queueRepo = new Mock<IQueueRepository>();
        
        var service = new NotificationService(notificationRepo, queueRepo.Object);

        var sendAt = DateTimeOffset.UtcNow.AddHours(1);
        var request = new SendNotificationRequest(
            Recipient: "test@example.com",
            TemplateKey: "welcome",
            Subject: null,
            Body: null,
            PayloadJson: "{\"name\":\"John\"}",
            Channel: ChannelType.Email,
            SendAt: sendAt,
            CustomerId: "customer123"
        );

        // Act
        var notificationId = await service.SendAsync(request);

        // Assert
        notificationId.Should().NotBeEmpty();
        
        // Verify queue item was enqueued with correct ReadyAt time
        queueRepo.Verify(q => q.EnqueueAsync(
            It.Is<NotificationQueueItem>(item => 
                item.NotificationId == notificationId && 
                item.ReadyAt == sendAt), 
            It.IsAny<CancellationToken>()), Times.Once);

        // Verify notification was persisted
        var notification = await context.Notifications.FirstAsync(n => n.Id == notificationId);
        notification.Recipient.Should().Be("test@example.com");
        notification.TemplateKey.Should().Be("welcome");
        notification.PayloadJson.Should().Be("{\"name\":\"John\"}");
        notification.Channel.Should().Be(ChannelType.Email);
        notification.CustomerId.Should().Be("customer123");
        notification.Status.Should().Be(NotificationStatus.Pending);
    }
}
