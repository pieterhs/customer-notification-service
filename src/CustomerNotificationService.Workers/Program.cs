using CustomerNotificationService.Workers.HostedServices;
using CustomerNotificationService.Infrastructure.Data;
using CustomerNotificationService.Application.Interfaces;
using CustomerNotificationService.Infrastructure.Repositories;
using CustomerNotificationService.Infrastructure.Providers;
using CustomerNotificationService.Workers.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

// EF Core Postgres
var connectionString = builder.Configuration.GetConnectionString("Default") ??
                       builder.Configuration["POSTGRES_CONNECTION"] ??
                       "Host=localhost;Port=5432;Database=notifications;Username=postgres;Password=postgres";
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

// Repositories
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<ITemplateRepository, TemplateRepository>();
builder.Services.AddScoped<IQueueRepository, QueueRepository>();

// Providers
builder.Services.AddScoped<INotificationProvider, MockEmailProvider>();
builder.Services.AddScoped<INotificationProvider, MockSmsProvider>();
builder.Services.AddScoped<INotificationProvider, MockPushProvider>();

// Options
builder.Services.Configure<RetryPolicyOptions>(builder.Configuration.GetSection("RetryPolicy"));

// Hosted services
builder.Services.AddHostedService<QueueWorker>();
builder.Services.AddHostedService<SchedulerWorker>();

var host = builder.Build();

// Apply EF Core migrations on startup (configurable)
var applyMigrations = builder.Configuration.GetValue<bool?>("ApplyMigrations") ?? true;
if (applyMigrations)
{
    using var scope = host.Services.CreateScope();
    var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
    var logger = loggerFactory.CreateLogger("Migrations");
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        logger.LogInformation("Applying database migrations (Workers)...");
        db.Database.Migrate();
        logger.LogInformation("Database migrations applied successfully (Workers).");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while applying database migrations (Workers).");
    }
}
else
{
    using var scope = host.Services.CreateScope();
    var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
    var logger = loggerFactory.CreateLogger("Migrations");
    logger.LogInformation("ApplyMigrations=false; skipping database migrations on startup (Workers).");
}
host.Run();