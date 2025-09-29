using CustomerNotificationService.Application.Services;
using CustomerNotificationService.Domain.Entities;
using CustomerNotificationService.Domain.Enums;
using CustomerNotificationService.Infrastructure.Data;
using CustomerNotificationService.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CustomerNotificationService.Tests;

public class IntegrationTests
{
    [Fact]
    public async Task EndToEnd_HappyPath_Test()
    {
        // Arrange - Set up in-memory database
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);
        
        // Add a sample template
        var template = new CustomerNotificationService.Domain.Entities.Template
        {
            Id = Guid.NewGuid(),
            Key = "welcome",
            Subject = "Welcome {{name}}!",
            Body = "Hello {{name}}, welcome to our service!",
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.Templates.Add(template);
        await context.SaveChangesAsync();

        var notificationRepo = new NotificationRepository(context);
        var queueRepo = new QueueRepository(context);
        var service = new NotificationService(notificationRepo, queueRepo);

        // Act - Send a notification
        var request = new SendNotificationRequest(
            Recipient: "john@example.com",
            TemplateKey: "welcome",
            Subject: null,
            Body: null,
            PayloadJson: "{\"name\":\"John\"}",
            Channel: ChannelType.Email,
            SendAt: null,
            CustomerId: "customer123"
        );

        var notificationId = await service.SendAsync(request);

        // Assert - Verify notification was created
        var notification = await context.Notifications.FirstAsync(n => n.Id == notificationId);
        notification.Recipient.Should().Be("john@example.com");
        notification.TemplateKey.Should().Be("welcome");
        notification.Status.Should().Be(NotificationStatus.Pending);

        // Verify queue item was created
        var queueItem = await context.NotificationQueue.FirstAsync(q => q.NotificationId == notificationId);
        queueItem.JobStatus.Should().Be("Pending");
        queueItem.ReadyAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(10));

        // Simulate queue processing - dequeue item
        var dequeuedItem = await queueRepo.DequeueAsync();
        dequeuedItem.Should().NotBeNull();
        dequeuedItem!.NotificationId.Should().Be(notificationId);
        dequeuedItem.JobStatus.Should().Be("Processing");

        // Complete the job
        await queueRepo.CompleteAsync(dequeuedItem.Id);
        
        // Verify completion
        var completedItem = await context.NotificationQueue.FirstAsync(q => q.Id == dequeuedItem.Id);
        completedItem.JobStatus.Should().Be("Completed");
        completedItem.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task QueueRepository_Should_HandleRetries()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);
        var queueRepo = new QueueRepository(context);

        var queueItem = new NotificationQueueItem
        {
            NotificationId = Guid.NewGuid(),
            ReadyAt = DateTimeOffset.UtcNow
        };

        await queueRepo.EnqueueAsync(queueItem);

        // Act - Dequeue and fail multiple times
        var item1 = await queueRepo.DequeueAsync();
        await queueRepo.FailAsync(item1!.Id, 1); // First failure

        var item2 = await queueRepo.DequeueAsync();
        item2.Should().BeNull(); // Should not be ready yet

        // Wait and try again (simulate retry delay)
        var storedItem = await context.NotificationQueue.FirstAsync(q => q.Id == item1!.Id);
        storedItem.ReadyAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        await context.SaveChangesAsync();

        var item3 = await queueRepo.DequeueAsync();
        item3.Should().NotBeNull();
        item3!.AttemptCount.Should().Be(2);

        await queueRepo.FailAsync(item3.Id, 2); // Second failure
        
        // Third attempt
        storedItem = await context.NotificationQueue.FirstAsync(q => q.Id == item3.Id);
        storedItem.ReadyAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        await context.SaveChangesAsync();
        
        var item4 = await queueRepo.DequeueAsync();
        item4.Should().NotBeNull();
        item4!.AttemptCount.Should().Be(3);
        
        await queueRepo.FailAsync(item4.Id, 4); // Third failure - should mark as permanently failed

        // Refresh the entity from database
        var finalItem = await context.NotificationQueue.FirstAsync(q => q.Id == item4.Id);
        finalItem.JobStatus.Should().Be("Failed");
        finalItem.CompletedAt.Should().NotBeNull();
    }
}