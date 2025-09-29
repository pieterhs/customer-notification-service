namespace CustomerNotificationService.Workers.HostedServices;

public class SchedulerWorker : BackgroundService
{
    private readonly ILogger<SchedulerWorker> _logger;

    public SchedulerWorker(ILogger<SchedulerWorker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SchedulerWorker started");
        while (!stoppingToken.IsCancellationRequested)
        {
            // TODO: move scheduled notifications into queue
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
