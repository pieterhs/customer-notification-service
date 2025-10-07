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
        var auditLogger = new Mock<IAuditLogger>();
        
        var service = new NotificationService(notificationRepo, queueRepo.Object, auditLogger.Object);

        var request = new SendNotificationRequest(
            Recipient: "test@example.com",
            TemplateKey: null,
            Subject: "Test Subject",
            Body: "Test Body",
            PayloadJson: null,
            Channel: ChannelType.Email
        );

        // Act
        var response = await service.SendAsync(request);

        // Assert
        response.NotificationId.Should().NotBeEmpty();
        response.IsExisting.Should().BeFalse();
        queueRepo.Verify(q => q.EnqueueAsync(It.IsAny<NotificationQueueItem>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_ScheduledNotification_Should_NotEnqueue_And_SetScheduled()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);
        var notificationRepo = new CustomerNotificationService.Infrastructure.Repositories.NotificationRepository(context);
        var queueRepo = new Mock<IQueueRepository>();
        var auditLogger = new Mock<IAuditLogger>();
        
        var service = new NotificationService(notificationRepo, queueRepo.Object, auditLogger.Object);

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
        var response = await service.SendAsync(request);

        // Assert
        response.NotificationId.Should().NotBeEmpty();
        response.IsExisting.Should().BeFalse();
        response.Status.Should().Be("Scheduled");
        
        // Verify queue item was NOT enqueued yet
        queueRepo.Verify(q => q.EnqueueAsync(It.IsAny<NotificationQueueItem>(), It.IsAny<CancellationToken>()), Times.Never);

        // Verify notification was persisted
        var notification = await context.Notifications.FirstAsync(n => n.Id == response.NotificationId);
        notification.Recipient.Should().Be("test@example.com");
        notification.TemplateKey.Should().Be("welcome");
        notification.PayloadJson.Should().Be("{\"name\":\"John\"}");
        notification.Channel.Should().Be(ChannelType.Email);
        notification.CustomerId.Should().Be("customer123");
        notification.Status.Should().Be(NotificationStatus.Scheduled);
    }

    [Fact]
    public async Task SendAsync_WithIdempotencyKey_Should_Return_ExistingNotification()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);
        var notificationRepo = new CustomerNotificationService.Infrastructure.Repositories.NotificationRepository(context);
        var queueRepo = new Mock<IQueueRepository>();
        var auditLogger = new Mock<IAuditLogger>();
        
        var service = new NotificationService(notificationRepo, queueRepo.Object, auditLogger.Object);

        var idempotencyKey = "test-key-123";
        var request = new SendNotificationRequest(
            Recipient: "test@example.com",
            TemplateKey: "welcome",
            Subject: null,
            Body: null,
            PayloadJson: "{\"name\":\"John\"}",
            Channel: ChannelType.Email,
            SendAt: null,
            CustomerId: "customer123",
            IdempotencyKey: idempotencyKey
        );

        // Act - Send first request
        var firstResponse = await service.SendAsync(request);
        
        // Act - Send second request with same idempotency key
        var secondResponse = await service.SendAsync(request);

        // Assert
        firstResponse.NotificationId.Should().NotBeEmpty();
        firstResponse.IsExisting.Should().BeFalse();
        firstResponse.IdempotencyKey.Should().Be(idempotencyKey);
        
        secondResponse.NotificationId.Should().Be(firstResponse.NotificationId);
        secondResponse.IsExisting.Should().BeTrue();
        secondResponse.IdempotencyKey.Should().Be(idempotencyKey);
        
        // Verify only one notification was created
        var notifications = await context.Notifications.ToListAsync();
        notifications.Should().HaveCount(1);
        notifications[0].IdempotencyKey.Should().Be(idempotencyKey);
        
        // Verify enqueue was called only once
        queueRepo.Verify(q => q.EnqueueAsync(It.IsAny<NotificationQueueItem>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
