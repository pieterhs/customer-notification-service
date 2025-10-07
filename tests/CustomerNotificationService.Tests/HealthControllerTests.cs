using CustomerNotificationService.Api.Controllers;
using CustomerNotificationService.Infrastructure.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CustomerNotificationService.Tests;

public class HealthControllerTests
{
    [Fact]
    public void Get_ShouldReturn_200OK_WithHealthyStatus()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);
        var controller = new HealthController(context);

        // Act
        var result = controller.Get();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
        
        // Verify the response contains status and time
        var response = okResult.Value.ToString();
        response.Should().Contain("status");
        response.Should().Contain("time");
    }

    [Fact]
    public async Task GetMetrics_WithValidDatabase_ShouldReturn_200OK()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);
        var controller = new HealthController(context);

        // Act
        var result = await controller.GetMetrics();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }
}