using CustomerNotificationService.Workers.HostedServices;
using CustomerNotificationService.Infrastructure.Data;
using CustomerNotificationService.Application.Interfaces;
using CustomerNotificationService.Infrastructure.Repositories;
using CustomerNotificationService.Infrastructure.Providers;
using Microsoft.EntityFrameworkCore;

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

// Hosted services
builder.Services.AddHostedService<QueueWorker>();
builder.Services.AddHostedService<SchedulerWorker>();

var host = builder.Build();
host.Run();