namespace CustomerNotificationService.Workers.Configuration;

public class RetryPolicyOptions
{
    public int MaxAttempts { get; set; } = 5;
    public int BaseBackoffSeconds { get; set; } = 30;
    public int MaxBackoffSeconds { get; set; } = 3600;

    public TimeSpan CalculateBackoff(int attemptCount)
    {
        var backoffSeconds = Math.Min(
            Math.Pow(2, attemptCount) * BaseBackoffSeconds,
            MaxBackoffSeconds
        );
        return TimeSpan.FromSeconds(backoffSeconds);
    }
}