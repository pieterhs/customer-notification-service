using CustomerNotificationService.Domain.Entities;
using FluentAssertions;

namespace CustomerNotificationService.Tests;

public class TemplateRenderingTests
{
    [Fact]
    public void Template_Should_Have_Defaults()
    {
        var t = new Template();
        t.Key.Should().BeEmpty();
        t.Subject.Should().BeEmpty();
        t.Body.Should().BeEmpty();
    }
}
