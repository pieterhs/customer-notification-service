using CustomerNotificationService.Api.Controllers;
using CustomerNotificationService.Application.Dtos;
using CustomerNotificationService.Application.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CustomerNotificationService.Tests;

public class AdminControllerTests
{
    [Fact]
    public async Task ListTemplates_ShouldReturn_Ok_WithItems()
    {
        var mock = new Mock<ITemplateService>();
        mock.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new TemplateDto { Id = Guid.NewGuid(), Name = "A" } });

        var controller = new AdminController(mock.Object);
        var result = await controller.ListTemplates(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var items = ok.Value as IEnumerable<TemplateDto>;
        items.Should().NotBeNull();
        items!.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetTemplate_WhenNotFound_ShouldReturn_404()
    {
        var mock = new Mock<ITemplateService>();
        mock.Setup(s => s.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TemplateDto?)null);

        var controller = new AdminController(mock.Object);
        var result = await controller.GetTemplate(Guid.NewGuid(), CancellationToken.None);
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task CreateTemplate_ShouldReturn_Created()
    {
        var created = new TemplateDto { Id = Guid.NewGuid(), Name = "New", Channel = "Email", Content = "x" };
        var mock = new Mock<ITemplateService>();
        mock.Setup(s => s.CreateAsync(It.IsAny<TemplateDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);

        var controller = new AdminController(mock.Object);
        var result = await controller.CreateTemplate(new TemplateDto { Name = "New", Channel = "Email", Content = "x" }, CancellationToken.None);
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.RouteValues!["id"].Should().Be(created.Id);
    }

    [Fact]
    public async Task UpdateTemplate_WhenNotFound_ShouldReturn_404()
    {
        var mock = new Mock<ITemplateService>();
        mock.Setup(s => s.UpdateAsync(It.IsAny<Guid>(), It.IsAny<TemplateDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var controller = new AdminController(mock.Object);
        var result = await controller.UpdateTemplate(Guid.NewGuid(), new TemplateDto { Name = "X" }, CancellationToken.None);
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task DeleteTemplate_WhenOk_ShouldReturn_NoContent()
    {
        var mock = new Mock<ITemplateService>();
        mock.Setup(s => s.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var controller = new AdminController(mock.Object);
        var result = await controller.DeleteTemplate(Guid.NewGuid(), CancellationToken.None);
        result.Should().BeOfType<NoContentResult>();
    }
}
