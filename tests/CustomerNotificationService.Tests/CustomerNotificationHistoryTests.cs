using CustomerNotificationService.Application.DTOs;
using CustomerNotificationService.Application.Common;
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

public class CustomerNotificationHistoryTests
{
    [Fact]
    public async Task GetCustomerNotificationHistoryAsync_WithFilters_Should_ReturnPagedResults()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);
        
        // Add test notifications
        var customerId = Guid.NewGuid();
        var notifications = new List<Notification>
        {
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId.ToString(),
                Recipient = "test1@example.com",
                Subject = "Test 1",
                Body = "Body 1",
                Channel = ChannelType.Email,
                Status = NotificationStatus.Sent,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-2)
            },
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId.ToString(),
                Recipient = "test2@example.com",
                Subject = "Test 2",
                Body = "Body 2",
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
                Channel = ChannelType.Email,
                Status = NotificationStatus.Pending,
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        context.Notifications.AddRange(notifications);
        await context.SaveChangesAsync();

        var notificationRepo = new NotificationRepository(context);
        var queueRepo = new Mock<IQueueRepository>();
        var auditLogger = new Mock<IAuditLogger>();
        var service = new NotificationService(notificationRepo, queueRepo.Object, auditLogger.Object);

        // Act
        var request = new CustomerNotificationHistoryRequest
        {
            CustomerId = customerId,
            Status = "Sent",
            Page = 1,
            PageSize = 10
        };

        var result = await service.GetCustomerNotificationHistoryAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        result.Items[0].Status.Should().Be("Sent");
        result.TotalItems.Should().Be(1);
        result.TotalPages.Should().Be(1);
        result.Page.Should().Be(1);
        result.HasNext.Should().BeFalse();
        result.HasPrevious.Should().BeFalse();
    }

    [Fact]
    public async Task GetCustomerNotificationHistoryAsync_WithPagination_Should_ReturnCorrectPage()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);
        
        var customerId = Guid.NewGuid();
        var notifications = Enumerable.Range(1, 25).Select(i => new Notification
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId.ToString(),
            Recipient = $"test{i}@example.com",
            Subject = $"Test {i}",
            Body = $"Body {i}",
            Channel = ChannelType.Email,
            Status = NotificationStatus.Sent,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-i)
        }).ToList();

        context.Notifications.AddRange(notifications);
        await context.SaveChangesAsync();

        var notificationRepo = new NotificationRepository(context);
        var queueRepo = new Mock<IQueueRepository>();
        var auditLogger = new Mock<IAuditLogger>();
        var service = new NotificationService(notificationRepo, queueRepo.Object, auditLogger.Object);

        // Act
        var request = new CustomerNotificationHistoryRequest
        {
            CustomerId = customerId,
            Page = 2,
            PageSize = 10
        };

        var result = await service.GetCustomerNotificationHistoryAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(10);
        result.TotalItems.Should().Be(25);
        result.TotalPages.Should().Be(3);
        result.Page.Should().Be(2);
        result.HasNext.Should().BeTrue();
        result.HasPrevious.Should().BeTrue();
    }

    [Fact]
    public async Task GetCustomerNotificationHistoryAsync_WithInvalidStatus_Should_ThrowArgumentException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);
        var notificationRepo = new NotificationRepository(context);
        var queueRepo = new Mock<IQueueRepository>();
        var auditLogger = new Mock<IAuditLogger>();
        var service = new NotificationService(notificationRepo, queueRepo.Object, auditLogger.Object);

        var request = new CustomerNotificationHistoryRequest
        {
            CustomerId = Guid.NewGuid(),
            Status = "InvalidStatus"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            service.GetCustomerNotificationHistoryAsync(request));
    }
}