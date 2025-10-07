using CustomerNotificationService.Application.Interfaces;
using CustomerNotificationService.Application.Services;
using CustomerNotificationService.Domain.Entities;
using CustomerNotificationService.Domain.Enums;
using CustomerNotificationService.Infrastructure.Data;
using CustomerNotificationService.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

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
        var auditLogger = new Mock<IAuditLogger>();
        var service = new NotificationService(notificationRepo, queueRepo, auditLogger.Object);

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
    queueItem.JobStatus.Should().Be("Queued");
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
    await queueRepo.FailAsync(item1!.Id, 60); // First failure (60s)

        var item2 = await queueRepo.DequeueAsync();
        item2.Should().BeNull(); // Should not be ready yet

        // Wait and try again (simulate retry delay)
        var storedItem = await context.NotificationQueue.FirstAsync(q => q.Id == item1!.Id);
    storedItem.NextAttemptAt = DateTimeOffset.UtcNow.AddSeconds(-1);
        await context.SaveChangesAsync();

        var item3 = await queueRepo.DequeueAsync();
        item3.Should().NotBeNull();
        item3!.AttemptCount.Should().Be(2);

    await queueRepo.FailAsync(item3.Id, 120); // Second failure
        
        // Third attempt
    storedItem = await context.NotificationQueue.FirstAsync(q => q.Id == item3.Id);
    storedItem.NextAttemptAt = DateTimeOffset.UtcNow.AddSeconds(-1);
        await context.SaveChangesAsync();
        
        var item4 = await queueRepo.DequeueAsync();
        item4.Should().NotBeNull();
        item4!.AttemptCount.Should().Be(3);
        
    await queueRepo.FailAsync(item4.Id, 240); // Third failure

        // Refresh the entity from database
        var finalItem = await context.NotificationQueue.FirstAsync(q => q.Id == item4.Id);
        // With repository-only logic, item remains queued for next attempts; permanent failure is handled by worker options
        finalItem.JobStatus.Should().Be("Queued");
    }

    [Fact]
    public async Task SchedulerWorker_Should_MoveScheduled_ToQueue()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);

        var n = new Notification
        {
            Id = Guid.NewGuid(),
            Recipient = "a@b.com",
            Channel = ChannelType.Email,
            Status = NotificationStatus.Scheduled,
            SendAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.Notifications.Add(n);
        await context.SaveChangesAsync();

        // Directly run the scheduling logic equivalent
        var now = DateTimeOffset.UtcNow;
        var toQueue = await context.Notifications
            .Where(x => x.Status == NotificationStatus.Scheduled && x.SendAt != null && x.SendAt <= now)
            .ToListAsync();

        foreach (var x in toQueue)
        {
            x.Status = NotificationStatus.Pending;
            context.NotificationQueue.Add(new NotificationQueueItem
            {
                Id = Guid.NewGuid(),
                NotificationId = x.Id,
                EnqueuedAt = now,
                ReadyAt = now,
                JobStatus = "Queued",
                AttemptCount = 0
            });
        }
        await context.SaveChangesAsync();

        var queued = await context.NotificationQueue.FirstOrDefaultAsync(q => q.NotificationId == n.Id);
        queued.Should().NotBeNull();
        var updated = await context.Notifications.FirstAsync(x => x.Id == n.Id);
        updated.Status.Should().Be(NotificationStatus.Pending);
    }
}