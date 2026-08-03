using Hangfire;

namespace WeatherWorkerDotNet;

/// <summary>
/// Registers the worker's recurring jobs once the host starts.
/// </summary>
public class RecurringJobScheduler : IHostedService
{
    private readonly IRecurringJobManager _recurringJobs;

    public RecurringJobScheduler(IRecurringJobManager recurringJobs)
    {
        _recurringJobs = recurringJobs;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _recurringJobs.AddOrUpdate<ConfirmNashvilleAIWeatherJob>(
            "confirm-nashville-ai-weather",
            job => job.RunAsync(),
            Cron.Daily(2));

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
