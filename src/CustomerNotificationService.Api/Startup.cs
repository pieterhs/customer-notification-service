using CustomerNotificationService.Infrastructure.Data;
using CustomerNotificationService.Application.Services;
using CustomerNotificationService.Application.Interfaces;
using CustomerNotificationService.Infrastructure.Repositories;
using CustomerNotificationService.Infrastructure.Providers;
using CustomerNotificationService.Api.Middleware;
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
    services.AddScoped<ITemplateService, TemplateService>();
    services.AddScoped<Application.Interfaces.IAuditLogger, Infrastructure.Services.AuditLogger>();

        // Repositories
    // Repository for admin-driven template management
    services.AddScoped<CustomerNotificationService.Application.Interfaces.INotificationTemplateRepository, NotificationTemplateRepository>();
    services.AddScoped<INotificationTemplateRepository, NotificationTemplateRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IQueueRepository, QueueRepository>();

        // Providers
        services.AddScoped<INotificationProvider, MockEmailProvider>();
        services.AddScoped<INotificationProvider, MockSmsProvider>();
        services.AddScoped<INotificationProvider, MockPushProvider>();

        // Swagger/OpenAPI
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo 
            { 
                Title = "Customer Notification Service API", 
                Version = "v1",
                Description = "A REST API for managing customer notifications with support for multiple channels, templates, scheduling, and delivery tracking."
            });
            
            // Include XML comments
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }
            
            // Describe all parameters in camelCase
            c.DescribeAllParametersInCamelCase();
            
            c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
            {
                Description = "API Key needed to access the endpoints. X-Api-Key: your-api-key",
                In = ParameterLocation.Header,
                Name = "X-Api-Key",
                Type = SecuritySchemeType.ApiKey
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKey" }
                    },
                    Array.Empty<string>()
                }
            });
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Customer Notification Service API v1");
        });

        app.UseMiddleware<ApiKeyMiddleware>();

        app.UseRouting();

        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}