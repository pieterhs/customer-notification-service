using CustomerNotificationService.Domain.Entities;
using CustomerNotificationService.Domain.Enums;
using CustomerNotificationService.Infrastructure.Data;
using CustomerNotificationService.Workers.Configuration;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CustomerNotificationService.Tests.Workers;

public class QueueWorkerTests
{
    [Fact]
    public void RetryPolicyOptions_Should_CalculateBackoff_WithExponentialGrowth()
    {
        // Arrange
        var retryPolicy = new RetryPolicyOptions
        {
            MaxAttempts = 5,
            BaseBackoffSeconds = 30,
            MaxBackoffSeconds = 3600
        };

        // Act & Assert
        var backoff1 = retryPolicy.CalculateBackoff(1);
        var backoff2 = retryPolicy.CalculateBackoff(2);
        var backoff3 = retryPolicy.CalculateBackoff(3);
        var backoff4 = retryPolicy.CalculateBackoff(4);
        var backoff5 = retryPolicy.CalculateBackoff(5);

        // Exponential: 2^1 * 30 = 60, 2^2 * 30 = 120, 2^3 * 30 = 240, etc.
        backoff1.TotalSeconds.Should().Be(60);
        backoff2.TotalSeconds.Should().Be(120);
        backoff3.TotalSeconds.Should().Be(240);
        backoff4.TotalSeconds.Should().Be(480);
        backoff5.TotalSeconds.Should().Be(960);
    }

    [Fact]
    public void RetryPolicyOptions_Should_CapBackoff_AtMaxSeconds()
    {
        // Arrange
        var retryPolicy = new RetryPolicyOptions
        {
            MaxAttempts = 10,
            BaseBackoffSeconds = 30,
            MaxBackoffSeconds = 600 // 10 minutes max
        };

        // Act
        var backoff10 = retryPolicy.CalculateBackoff(10); // 2^10 * 30 = 30720 seconds

        // Assert - should be capped at MaxBackoffSeconds
        backoff10.TotalSeconds.Should().Be(600);
    }

    [Fact]
    public async Task DeliveryAttempt_Logging_Should_RecordRetryInformation()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);

        var notificationId = Guid.NewGuid();
        var retryPolicy = new RetryPolicyOptions();

        // Act - Simulate logging multiple delivery attempts
        var attempt1 = new DeliveryAttempt
        {
            Id = Guid.NewGuid(),
            NotificationId = notificationId,
            AttemptedAt = DateTimeOffset.UtcNow,
            Success = false,
            Status = "Failed (retry)",
            ErrorMessage = "Provider returned error",
            RetryAfterSeconds = (int)retryPolicy.CalculateBackoff(1).TotalSeconds
        };

        var attempt2 = new DeliveryAttempt
        {
            Id = Guid.NewGuid(),
            NotificationId = notificationId,
            AttemptedAt = DateTimeOffset.UtcNow.AddMinutes(1),
            Success = false,
            Status = "Failed (retry)",
            ErrorMessage = "Provider still failing",
            RetryAfterSeconds = (int)retryPolicy.CalculateBackoff(2).TotalSeconds
        };

        var attempt3 = new DeliveryAttempt
        {
            Id = Guid.NewGuid(),
            NotificationId = notificationId,
            AttemptedAt = DateTimeOffset.UtcNow.AddMinutes(3),
            Success = true,
            Status = "Success",
            ResponseMessage = "Delivered successfully"
        };

        context.DeliveryAttempts.AddRange(attempt1, attempt2, attempt3);
        await context.SaveChangesAsync();

        // Assert
        var attempts = await context.DeliveryAttempts
            .Where(da => da.NotificationId == notificationId)
            .OrderBy(da => da.AttemptedAt)
            .ToListAsync();

        attempts.Should().HaveCount(3);
        
        attempts[0].Status.Should().Be("Failed (retry)");
        attempts[0].RetryAfterSeconds.Should().Be(60);
        
        attempts[1].Status.Should().Be("Failed (retry)");
        attempts[1].RetryAfterSeconds.Should().Be(120);
        
        attempts[2].Status.Should().Be("Success");
        attempts[2].Success.Should().BeTrue();
    }

    [Fact]
    public async Task QueueDequeue_Logic_Should_RespectNextAttemptAt()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);

        var now = DateTimeOffset.UtcNow;

        // Create queue item that's not ready for retry yet
        var queueItem = new NotificationQueueItem
        {
            Id = Guid.NewGuid(),
            NotificationId = Guid.NewGuid(),
            EnqueuedAt = now.AddMinutes(-5),
            ReadyAt = now.AddMinutes(-5),
            JobStatus = "Queued",
            AttemptCount = 1,
            NextAttemptAt = now.AddMinutes(5) // Not ready yet
        };

        context.NotificationQueue.Add(queueItem);
        await context.SaveChangesAsync();

        // Act - Simulate dequeue logic
        var readyItems = await context.NotificationQueue
            .Where(q => q.JobStatus == "Queued" && q.ReadyAt <= now && (q.NextAttemptAt == null || q.NextAttemptAt <= now))
            .ToListAsync();

        // Assert - should find no ready items
        readyItems.Should().BeEmpty();

        // Now make it ready
        queueItem.NextAttemptAt = now.AddMinutes(-1);
        await context.SaveChangesAsync();

        var readyItemsAfter = await context.NotificationQueue
            .Where(q => q.JobStatus == "Queued" && q.ReadyAt <= now && (q.NextAttemptAt == null || q.NextAttemptAt <= now))
            .ToListAsync();

        // Assert - should now find the item
        readyItemsAfter.Should().HaveCount(1);
        readyItemsAfter[0].Id.Should().Be(queueItem.Id);
    }
}