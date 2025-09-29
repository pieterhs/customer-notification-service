using CustomerNotificationService.Infrastructure.Data;
using CustomerNotificationService.Application.Services;
using CustomerNotificationService.Application.Interfaces;
using CustomerNotificationService.Infrastructure.Repositories;
using CustomerNotificationService.Infrastructure.Providers;
using CustomerNotificationService.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

namespace CustomerNotificationService.Api;

public class Startup
{
    private readonly IConfiguration _configuration;

    public Startup(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        // Controllers
        services.AddControllers();

        // EF Core - PostgreSQL
        var connectionString = _configuration.GetConnectionString("Default") ??
                                _configuration["POSTGRES_CONNECTION"] ??
                                "Host=localhost;Port=5432;Database=notifications;Username=postgres;Password=postgres";
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Application services
        services.AddScoped<INotificationService, NotificationService>();

        // Repositories
        services.AddScoped<ITemplateRepository, TemplateRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();

    // Queue service (in-memory placeholder)
    services.AddSingleton<IQueueService, InMemoryQueueService>();

        // Providers
        services.AddScoped<INotificationProvider, MockEmailProvider>();
        services.AddScoped<INotificationProvider, MockSmsProvider>();
        services.AddScoped<INotificationProvider, MockPushProvider>();

        // Swagger/OpenAPI
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Customer Notification Service API", Version = "v1" });
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Customer Notification Service API v1");
            });
        }

        app.UseRouting();

        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}
