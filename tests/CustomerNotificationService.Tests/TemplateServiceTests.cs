using CustomerNotificationService.Application.Dtos;
using CustomerNotificationService.Application.Services;
using CustomerNotificationService.Infrastructure.Data;
using CustomerNotificationService.Application.Interfaces;
using CustomerNotificationService.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CustomerNotificationService.Tests;

public class TemplateServiceTests
{
    private static AppDbContext InMemoryDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Create_Then_GetById_ShouldReturn_Template()
    {
        using var db = InMemoryDb();
        INotificationTemplateRepository repo = new NotificationTemplateRepository(db);
        var svc = new TemplateService(repo);

        var created = await svc.CreateAsync(new TemplateDto { Name = "Welcome", Channel = "Email", Content = "Hello" }, CancellationToken.None);
        created.Id.Should().NotBeEmpty();

        var fetched = await svc.GetByIdAsync(created.Id, CancellationToken.None);
        fetched.Should().NotBeNull();
        fetched!.Name.Should().Be("Welcome");
        fetched.Channel.Should().Be("Email");
    }

    [Fact]
    public async Task Update_ShouldModify_ExistingTemplate()
    {
        using var db = InMemoryDb();
        INotificationTemplateRepository repo = new NotificationTemplateRepository(db);
        var svc = new TemplateService(repo);

        var created = await svc.CreateAsync(new TemplateDto { Name = "A", Channel = "Sms", Content = "x" }, CancellationToken.None);
        var ok = await svc.UpdateAsync(created.Id, new TemplateDto { Name = "B", Content = "y" }, CancellationToken.None);
        ok.Should().BeTrue();

        var updated = await svc.GetByIdAsync(created.Id, CancellationToken.None);
        updated!.Name.Should().Be("B");
        updated.Content.Should().Be("y");
    }

    [Fact]
    public async Task Delete_ShouldRemove_Template()
    {
        using var db = InMemoryDb();
        INotificationTemplateRepository repo = new NotificationTemplateRepository(db);
        var svc = new TemplateService(repo);

        var created = await svc.CreateAsync(new TemplateDto { Name = "T", Channel = "Push", Content = "z" }, CancellationToken.None);
        (await svc.DeleteAsync(created.Id, CancellationToken.None)).Should().BeTrue();
        (await svc.GetByIdAsync(created.Id, CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task GetAll_ShouldReturn_AllTemplates()
    {
        using var db = InMemoryDb();
        INotificationTemplateRepository repo = new NotificationTemplateRepository(db);
        var svc = new TemplateService(repo);

        await svc.CreateAsync(new TemplateDto { Name = "1", Channel = "Email", Content = "a" }, CancellationToken.None);
        await svc.CreateAsync(new TemplateDto { Name = "2", Channel = "Email", Content = "b" }, CancellationToken.None);

        var list = (await svc.GetAllAsync(CancellationToken.None)).ToList();
        list.Should().HaveCount(2);
    }
}
