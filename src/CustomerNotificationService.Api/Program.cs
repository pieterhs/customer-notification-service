using Serilog;
using CustomerNotificationService.Api;
using CustomerNotificationService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog from appsettings
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// Use Startup for service and middleware wiring
var startup = new Startup(builder.Configuration);
startup.ConfigureServices(builder.Services);

var app = builder.Build();

// Apply EF Core migrations on startup (configurable)
var applyMigrations = builder.Configuration.GetValue<bool?>("ApplyMigrations") ?? true;
if (applyMigrations)
{
    try
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Log.Information("Applying database migrations...");
        db.Database.Migrate();
        Log.Information("Database migrations applied successfully.");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while applying database migrations.");
    }
}
else
{
    Log.Information("ApplyMigrations=false; skipping database migrations on startup.");
}

startup.Configure(app, app.Environment);

// Add health check endpoints
app.MapGet("/health/live", [AllowAnonymous] () => Results.Text("Healthy"))
    .WithName("HealthLive")
    .WithOpenApi();

app.MapGet("/health/ready", [AllowAnonymous] async (AppDbContext db) =>
{
    try
    {
        var canConnect = await db.Database.CanConnectAsync();
        return canConnect ? Results.Text("Healthy") : Results.Text("Unhealthy");
    }
    catch
    {
        return Results.Text("Unhealthy");
    }
})
.WithName("HealthReady")
.WithOpenApi();

app.Run();
