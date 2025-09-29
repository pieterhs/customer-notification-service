using CustomerNotificationService.Application.Interfaces;

namespace CustomerNotificationService.Workers.HostedServices;

public class QueueWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<QueueWorker> _logger;

    public QueueWorker(IServiceProvider services, ILogger<QueueWorker> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("QueueWorker started");
        while (!stoppingToken.IsCancellationRequested)
        {
            // TODO: dequeue and process items
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
