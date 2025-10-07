using CustomerNotificationService.Application.DTOs;
using CustomerNotificationService.Application.Common;
using CustomerNotificationService.Domain.Entities;
using CustomerNotificationService.Domain.Enums;
using CustomerNotificationService.Infrastructure.Data;
using CustomerNotificationService.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CustomerNotificationService.Tests;

public class NotificationRepositoryTests
{
    [Fact]
    public async Task GetCustomerNotificationHistoryAsync_ShouldReturnPagedResults_WithCorrectFiltering()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);
        var customerId = Guid.NewGuid();
        
        // Add test notifications with different statuses and dates
        var notifications = new List<Notification>
        {
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId.ToString(),
                Recipient = "test1@example.com",
                Subject = "Test 1",
                Body = "This is a test body that is long enough to test the preview functionality",
                TemplateKey = "template1",
                Channel = ChannelType.Email,
                Status = NotificationStatus.Sent,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-2),
                SentAt = DateTimeOffset.UtcNow.AddDays(-2).AddHours(1)
            },
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId.ToString(),
                Recipient = "test2@example.com",
                Subject = "Test 2",
                Body = "Body 2",
                TemplateKey = "template2",
                Channel = ChannelType.Sms,
                Status = NotificationStatus.Failed,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-1)
            },
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId.ToString(),
                Recipient = "test3@example.com",
                Subject = "Test 3",
                Body = "Body 3",
                TemplateKey = "template3",
                Channel = ChannelType.Email,
                Status = NotificationStatus.Pending,
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        // Add some delivery attempts
        var attempts = new List<DeliveryAttempt>
        {
            new()
            {
                Id = Guid.NewGuid(),
                NotificationId = notifications[0].Id,
                Success = true,
                AttemptedAt = DateTimeOffset.UtcNow.AddDays(-2).AddHours(1)
            },
            new()
            {
                Id = Guid.NewGuid(),
                NotificationId = notifications[1].Id,
                Success = false,
                ErrorMessage = "Failed to send SMS",
                AttemptedAt = DateTimeOffset.UtcNow.AddDays(-1).AddHours(1)
            }
        };

        context.Notifications.AddRange(notifications);
        context.DeliveryAttempts.AddRange(attempts);
        await context.SaveChangesAsync();

        var repository = new NotificationRepository(context);

        // Act - Test basic filtering and pagination
        var request = new CustomerNotificationHistoryRequest
        {
            CustomerId = customerId,
            Page = 1,
            PageSize = 10
        };

        var result = await repository.GetCustomerNotificationHistoryAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(3);
        result.TotalItems.Should().Be(3);
        result.TotalPages.Should().Be(1);
        result.Page.Should().Be(1);
        result.HasNext.Should().BeFalse();
        result.HasPrevious.Should().BeFalse();

        // Verify ordering (should be CreatedAt descending)
        result.Items[0].CreatedAt.Should().BeAfter(result.Items[1].CreatedAt);
        result.Items[1].CreatedAt.Should().BeAfter(result.Items[2].CreatedAt);

        // Verify data mapping
        var sentItem = result.Items.FirstOrDefault(i => i.Status == "Sent");
        sentItem.Should().NotBeNull();
        sentItem!.AttemptCount.Should().Be(1);
        sentItem.LastError.Should().BeNull();
        sentItem.RenderedPreview.Should().Contain("Test 1");

        var failedItem = result.Items.FirstOrDefault(i => i.Status == "Failed");
        failedItem.Should().NotBeNull();
        failedItem!.AttemptCount.Should().Be(1);
        failedItem.LastError.Should().Be("Failed to send SMS");
    }

    [Fact]
    public async Task GetCustomerNotificationHistoryAsync_ShouldFilterByStatus()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);
        var customerId = Guid.NewGuid();
        
        var notifications = new List<Notification>
        {
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId.ToString(),
                Recipient = "test1@example.com",
                Subject = "Sent Notification",
                Channel = ChannelType.Email,
                Status = NotificationStatus.Sent,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-1)
            },
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId.ToString(),
                Recipient = "test2@example.com",
                Subject = "Failed Notification",
                Channel = ChannelType.Email,
                Status = NotificationStatus.Failed,
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        context.Notifications.AddRange(notifications);
        await context.SaveChangesAsync();

        var repository = new NotificationRepository(context);

        // Act - Filter by "Sent" status
        var request = new CustomerNotificationHistoryRequest
        {
            CustomerId = customerId,
            Status = "Sent",
            Page = 1,
            PageSize = 10
        };

        var result = await repository.GetCustomerNotificationHistoryAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Items[0].Status.Should().Be("Sent");
        result.Items[0].RenderedPreview.Should().Contain("Sent Notification");
    }

    [Fact]
    public async Task GetCustomerNotificationHistoryAsync_ShouldFilterByDateRange()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);
        var customerId = Guid.NewGuid();
        
        var baseDate = DateTimeOffset.UtcNow.Date;
        var notifications = new List<Notification>
        {
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId.ToString(),
                Recipient = "old@example.com",
                Subject = "Old Notification",
                Channel = ChannelType.Email,
                Status = NotificationStatus.Sent,
                CreatedAt = baseDate.AddDays(-5)
            },
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId.ToString(),
                Recipient = "recent@example.com",
                Subject = "Recent Notification",
                Channel = ChannelType.Email,
                Status = NotificationStatus.Sent,
                CreatedAt = baseDate.AddDays(-1)
            }
        };

        context.Notifications.AddRange(notifications);
        await context.SaveChangesAsync();

        var repository = new NotificationRepository(context);

        // Act - Filter by date range (last 3 days)
        var request = new CustomerNotificationHistoryRequest
        {
            CustomerId = customerId,
            From = baseDate.AddDays(-3),
            To = baseDate,
            Page = 1,
            PageSize = 10
        };

        var result = await repository.GetCustomerNotificationHistoryAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Items[0].RenderedPreview.Should().Contain("Recent");
    }
}