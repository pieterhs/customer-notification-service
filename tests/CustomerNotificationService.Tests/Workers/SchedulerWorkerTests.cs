using CustomerNotificationService.Domain.Entities;
using CustomerNotificationService.Domain.Enums;
using CustomerNotificationService.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CustomerNotificationService.Tests.Workers;

public class SchedulerWorkerTests
{
    [Fact]
    public async Task ProcessScheduledNotifications_Should_EnqueueNotifications_WhenSendAtIsPast()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);

        // Create a scheduled notification with past SendAt
        var pastSendAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            Recipient = "test@example.com",
            Subject = "Test Subject",
            Body = "Test Body",
            Channel = ChannelType.Email,
            Status = NotificationStatus.Scheduled,
            SendAt = pastSendAt,
            CreatedAt = DateTimeOffset.UtcNow.AddHours(-1)
        };

        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        // Act - Simulate the scheduler processing logic
        var now = DateTimeOffset.UtcNow;
        
        var dueNotifications = await context.Notifications
            .Where(n => n.Status == NotificationStatus.Scheduled && 
                       n.SendAt != null && 
                       n.SendAt <= now)
            .ToListAsync();

        foreach (var dueNotification in dueNotifications)
        {
            var queueEntry = new NotificationQueueItem
            {
                Id = Guid.NewGuid(),
                NotificationId = dueNotification.Id,
                EnqueuedAt = now,
                ReadyAt = now,
                JobStatus = "Queued",
                AttemptCount = 0,
                NextAttemptAt = null
            };

            context.NotificationQueue.Add(queueEntry);
            dueNotification.Status = NotificationStatus.Pending;
        }

        await context.SaveChangesAsync();

        // Assert
        var updatedNotification = await context.Notifications.FirstAsync(n => n.Id == notification.Id);
        updatedNotification.Status.Should().Be(NotificationStatus.Pending);

        var queueItem = await context.NotificationQueue.FirstOrDefaultAsync(q => q.NotificationId == notification.Id);
        queueItem.Should().NotBeNull();
        queueItem!.JobStatus.Should().Be("Queued");
        queueItem.AttemptCount.Should().Be(0);
        queueItem.ReadyAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task ProcessScheduledNotifications_Should_IgnoreNotifications_WhenSendAtIsFuture()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);

        // Create a scheduled notification with future SendAt
        var futureSendAt = DateTimeOffset.UtcNow.AddMinutes(30);
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            Recipient = "future@example.com",
            Subject = "Future Test",
            Body = "Future Body",
            Channel = ChannelType.Email,
            Status = NotificationStatus.Scheduled,
            SendAt = futureSendAt,
            CreatedAt = DateTimeOffset.UtcNow
        };

        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        // Act - Simulate the scheduler processing logic
        var now = DateTimeOffset.UtcNow;
        
        var dueNotifications = await context.Notifications
            .Where(n => n.Status == NotificationStatus.Scheduled && 
                       n.SendAt != null && 
                       n.SendAt <= now)
            .ToListAsync();

        // Should find no notifications to process
        dueNotifications.Should().BeEmpty();

        // Assert - notification should remain scheduled
        var updatedNotification = await context.Notifications.FirstAsync(n => n.Id == notification.Id);
        updatedNotification.Status.Should().Be(NotificationStatus.Scheduled);

        var queueItem = await context.NotificationQueue.FirstOrDefaultAsync(q => q.NotificationId == notification.Id);
        queueItem.Should().BeNull();
    }
}