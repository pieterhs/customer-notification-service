using CustomerNotificationService.Api.Controllers;
using CustomerNotificationService.Application.Interfaces;
using CustomerNotificationService.Application.Services;
using CustomerNotificationService.Application.DTOs;
using CustomerNotificationService.Application.Common;
using CustomerNotificationService.Domain.Entities;
using CustomerNotificationService.Domain.Enums;
using CustomerNotificationService.Infrastructure.Data;
using CustomerNotificationService.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace CustomerNotificationService.Tests;

public class NotificationsControllerTests
{
    private NotificationsController CreateController(AppDbContext context)
    {
        var notificationRepo = new NotificationRepository(context);
        var queueRepo = new Mock<IQueueRepository>();
        var auditLogger = new Mock<IAuditLogger>();
        var notificationService = new NotificationService(notificationRepo, queueRepo.Object, auditLogger.Object);
        var logger = new Mock<ILogger<NotificationsController>>();
        
        return new NotificationsController(notificationService, context, logger.Object);
    }

    [Fact]
    public async Task Send_WithValidRequest_ShouldReturn_202Accepted()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);
        var controller = CreateController(context);

        var request = new SendNotificationRequest(
            Recipient: "test@example.com",
            TemplateKey: "welcome",
            Subject: null,
            Body: null,
            PayloadJson: "{\"name\":\"John\"}",
            Channel: ChannelType.Email,
            SendAt: null,
            CustomerId: "customer123"
        );

        // Act
        var result = await controller.Send(request);

        // Assert
        var acceptedResult = result.Should().BeOfType<AcceptedResult>().Subject;
        var response = acceptedResult.Value.Should().BeOfType<SendNotificationResponse>().Subject;
        response.NotificationId.Should().NotBeEmpty();
        response.Status.Should().Be("Pending");
        response.IsExisting.Should().BeFalse();
    }

    [Fact]
    public async Task Send_WithIdempotencyKey_FirstRequest_ShouldReturn_202Accepted()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);
        var controller = CreateController(context);

        var request = new SendNotificationRequest(
            Recipient: "test@example.com",
            TemplateKey: "welcome",
            Subject: null,
            Body: null,
            PayloadJson: "{\"name\":\"John\"}",
            Channel: ChannelType.Email,
            SendAt: null,
            CustomerId: "customer123"
        );
        var idempotencyKey = "test-key-123";

        // Act
        var result = await controller.Send(request, idempotencyKey);

        // Assert
        var acceptedResult = result.Should().BeOfType<AcceptedResult>().Subject;
        var response = acceptedResult.Value.Should().BeOfType<SendNotificationResponse>().Subject;
        response.NotificationId.Should().NotBeEmpty();
        response.Status.Should().Be("Pending");
        response.IdempotencyKey.Should().Be(idempotencyKey);
        response.IsExisting.Should().BeFalse();
    }

    [Fact]
    public async Task Send_WithIdempotencyKey_SecondRequest_ShouldReturn_200OK_WithSameId()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);
        var controller = CreateController(context);

        var request = new SendNotificationRequest(
            Recipient: "test@example.com",
            TemplateKey: "welcome",
            Subject: null,
            Body: null,
            PayloadJson: "{\"name\":\"John\"}",
            Channel: ChannelType.Email,
            SendAt: null,
            CustomerId: "customer123"
        );
        var idempotencyKey = "test-key-123";

        // Act - First request
        var firstResult = await controller.Send(request, idempotencyKey);
        var firstResponse = ((AcceptedResult)firstResult).Value as SendNotificationResponse;

        // Act - Second request with same key
        var secondResult = await controller.Send(request, idempotencyKey);

        // Assert
        var okResult = secondResult.Should().BeOfType<OkObjectResult>().Subject;
        var secondResponse = okResult.Value.Should().BeOfType<SendNotificationResponse>().Subject;
        
        secondResponse.NotificationId.Should().Be(firstResponse!.NotificationId);
        secondResponse.IdempotencyKey.Should().Be(idempotencyKey);
        secondResponse.IsExisting.Should().BeTrue();
        
        // Verify only one notification was created
        var notifications = await context.Notifications.ToListAsync();
        notifications.Should().HaveCount(1);
    }

    [Fact]
    public async Task Send_WithInvalidRequest_ShouldReturn_400BadRequest()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);
        var controller = CreateController(context);

        var request = new SendNotificationRequest(
            Recipient: "", // Invalid - empty recipient
            TemplateKey: null,
            Subject: null,
            Body: null,
            PayloadJson: null,
            Channel: ChannelType.Email
        );

        // Act
        var result = await controller.Send(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetHistory_WithValidCustomerId_ShouldReturn_200OK_WithPagedResults()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);
        var controller = CreateController(context);
        var customerId = Guid.NewGuid().ToString();

        // Create test notifications
        var notifications = new List<Notification>
        {
            new() {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                Recipient = "test1@example.com",
                Subject = "Test 1",
                Body = "Test Body 1",
                Channel = ChannelType.Email,
                Status = NotificationStatus.Sent,
                CreatedAt = DateTimeOffset.UtcNow.AddHours(-2),
                SentAt = DateTimeOffset.UtcNow.AddHours(-1)
            },
            new() {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                Recipient = "test2@example.com",
                Subject = "Test 2",
                Body = "Test Body 2",
                Channel = ChannelType.Email,
                Status = NotificationStatus.Pending,
                CreatedAt = DateTimeOffset.UtcNow.AddHours(-1)
            }
        };

        context.Notifications.AddRange(notifications);
        await context.SaveChangesAsync();

        // Act
        var result = await controller.GetHistory(customerId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var pagedResult = okResult.Value.Should().BeOfType<PagedResult<CustomerNotificationHistoryItemDto>>().Subject;
        
        pagedResult.Items.Should().HaveCount(2);
        pagedResult.Page.Should().Be(1);
        pagedResult.PageSize.Should().Be(20);
        pagedResult.TotalItems.Should().Be(2);
        pagedResult.TotalPages.Should().Be(1);
        pagedResult.HasNext.Should().BeFalse();
        pagedResult.HasPrevious.Should().BeFalse();
    }

    [Fact]
    public async Task GetHistory_WithStatusFilter_ShouldReturn_FilteredResults()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);
        var controller = CreateController(context);
        var customerId = Guid.NewGuid().ToString();

        // Create test notifications with different statuses
        var notifications = new List<Notification>
        {
            new() {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                Recipient = "test1@example.com",
                Subject = "Sent Notification",
                Body = "Test Body",
                Channel = ChannelType.Email,
                Status = NotificationStatus.Sent,
                CreatedAt = DateTimeOffset.UtcNow,
                SentAt = DateTimeOffset.UtcNow
            },
            new() {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                Recipient = "test2@example.com",
                Subject = "Pending Notification",
                Body = "Test Body",
                Channel = ChannelType.Email,
                Status = NotificationStatus.Pending,
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        context.Notifications.AddRange(notifications);
        await context.SaveChangesAsync();

        // Act
        var result = await controller.GetHistory(customerId, status: "Sent");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var pagedResult = okResult.Value.Should().BeOfType<PagedResult<CustomerNotificationHistoryItemDto>>().Subject;
        
        pagedResult.Items.Should().HaveCount(1);
        pagedResult.Items.First().Status.Should().Be("Sent");
    }

    [Fact]
    public async Task GetHistory_WithDateRangeFilter_ShouldReturn_FilteredResults()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);
        var controller = CreateController(context);
        var customerId = Guid.NewGuid().ToString();

        var baseDate = DateTimeOffset.UtcNow;
        var notifications = new List<Notification>
        {
            new() {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                Recipient = "test1@example.com",
                Subject = "Old Notification",
                Body = "Test Body",
                Channel = ChannelType.Email,
                Status = NotificationStatus.Sent,
                CreatedAt = baseDate.AddDays(-10), // Outside range
                SentAt = baseDate.AddDays(-10)
            },
            new() {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                Recipient = "test2@example.com",
                Subject = "Recent Notification",
                Body = "Test Body",
                Channel = ChannelType.Email,
                Status = NotificationStatus.Sent,
                CreatedAt = baseDate.AddDays(-1), // Within range
                SentAt = baseDate.AddDays(-1)
            }
        };

        context.Notifications.AddRange(notifications);
        await context.SaveChangesAsync();

        // Act
        var from = baseDate.AddDays(-2);
        var to = baseDate;
        var result = await controller.GetHistory(customerId, from: from, to: to);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var pagedResult = okResult.Value.Should().BeOfType<PagedResult<CustomerNotificationHistoryItemDto>>().Subject;
        
        pagedResult.Items.Should().HaveCount(1);
        pagedResult.Items.First().Status.Should().Be("Sent");
    }

    [Fact]
    public async Task GetHistory_WithCustomPageSize_ShouldReturn_LimitedResults()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);
        var controller = CreateController(context);
        var customerId = Guid.NewGuid().ToString();

        // Create 5 test notifications
        var notifications = Enumerable.Range(1, 5).Select(i => new Notification
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Recipient = $"test{i}@example.com",
            Subject = $"Test {i}",
            Body = "Test Body",
            Channel = ChannelType.Email,
            Status = NotificationStatus.Sent,
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-i),
            SentAt = DateTimeOffset.UtcNow.AddMinutes(-i)
        }).ToList();

        context.Notifications.AddRange(notifications);
        await context.SaveChangesAsync();

        // Act
        var result = await controller.GetHistory(customerId, pageSize: 3);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var pagedResult = okResult.Value.Should().BeOfType<PagedResult<CustomerNotificationHistoryItemDto>>().Subject;
        
        pagedResult.Items.Should().HaveCount(3);
        pagedResult.PageSize.Should().Be(3);
        pagedResult.TotalItems.Should().Be(5);
        pagedResult.TotalPages.Should().Be(2);
        pagedResult.HasNext.Should().BeTrue();
        pagedResult.HasPrevious.Should().BeFalse();
    }

    [Fact]
    public async Task GetHistory_WithInvalidPageSize_ShouldReturn_400BadRequest()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);
        var controller = CreateController(context);
        var customerId = Guid.NewGuid().ToString();

        // Act
        var result = await controller.GetHistory(customerId, pageSize: 0); // Invalid page size

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetHistory_WithInvalidPage_ShouldReturn_400BadRequest()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);
        var controller = CreateController(context);
        var customerId = Guid.NewGuid().ToString();

        // Act
        var result = await controller.GetHistory(customerId, page: 0); // Invalid page

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetHistory_WithInvalidCustomerId_ShouldReturn_400BadRequest()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);
        var controller = CreateController(context);

        // Act
        var result = await controller.GetHistory("invalid-guid");

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetHistory_WithEmptyCustomerId_ShouldReturn_400BadRequest()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);
        var controller = CreateController(context);

        // Act
        var result = await controller.GetHistory("");

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetHistory_WithInvalidDateRange_ShouldReturn_400BadRequest()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);
        var controller = CreateController(context);
        var customerId = Guid.NewGuid().ToString();

        var from = DateTimeOffset.UtcNow;
        var to = DateTimeOffset.UtcNow.AddDays(-1); // To is before From

        // Act
        var result = await controller.GetHistory(customerId, from: from, to: to);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetHistory_WithNoNotifications_ShouldReturn_404NotFound()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);
        var controller = CreateController(context);
        var customerId = Guid.NewGuid().ToString();

        // Act
        var result = await controller.GetHistory(customerId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }
}