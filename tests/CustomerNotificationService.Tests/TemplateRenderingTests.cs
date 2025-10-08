using CustomerNotificationService.Domain.Entities;
using FluentAssertions;
using Scriban;

namespace CustomerNotificationService.Tests;

public class TemplateRenderingTests
{
    [Fact]
    public void NotificationTemplate_Should_Have_Defaults()
    {
        var t = new NotificationTemplate();
        t.Name.Should().BeNull();
        t.Channel.Should().BeNull();
        t.Content.Should().BeNull();
        t.Id.Should().Be(Guid.Empty);
        t.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        t.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public async Task NotificationTemplate_Should_Render_Placeholders()
    {
        // Arrange
        var contentTemplate = Scriban.Template.Parse("Welcome {{name}}, your order {{order_id}} is ready.");
        
        var data = new { name = "John", order_id = "12345" };

        // Act
        var renderedContent = await contentTemplate.RenderAsync(data);

        // Assert
        renderedContent.Should().Be("Welcome John, your order 12345 is ready.");
    }
}
