using CustomerNotificationService.Domain.Entities;
using FluentAssertions;
using Scriban;

namespace CustomerNotificationService.Tests;

public class TemplateRenderingTests
{
    [Fact]
    public void Template_Should_Have_Defaults()
    {
        var t = new CustomerNotificationService.Domain.Entities.Template();
        t.Key.Should().BeEmpty();
        t.Subject.Should().BeEmpty();
        t.Body.Should().BeEmpty();
    }

    [Fact]
    public async Task Template_Should_Render_Placeholders()
    {
        // Arrange
        var subjectTemplate = Scriban.Template.Parse("Hello {{name}}!");
        var bodyTemplate = Scriban.Template.Parse("Welcome {{name}}, your order {{order_id}} is ready.");
        
        var data = new { name = "John", order_id = "12345" };

        // Act
        var renderedSubject = await subjectTemplate.RenderAsync(data);
        var renderedBody = await bodyTemplate.RenderAsync(data);

        // Assert
        renderedSubject.Should().Be("Hello John!");
        renderedBody.Should().Be("Welcome John, your order 12345 is ready.");
    }
}
