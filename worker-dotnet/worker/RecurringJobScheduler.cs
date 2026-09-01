using Core.AIWeather.Events;
using Core.Hangfire;
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
        _recurringJobs.RemoveIfExists("");

        _recurringJobs.AddOrUpdateCQMediatorEvent(
            "confirm-nashville-ai-weather-v3",
            Cron.Daily(2),
            new ConfirmNashvilleAIWeatherEvent { Version = 3 },
            queue: "batch-multi");

        _recurringJobs.AddOrUpdateCQMediatorEvent(
            "confirm-nashville-ai-weather-v4",
            Cron.Daily(2),
            new ConfirmNashvilleAIWeatherEvent { Version = 4 },
            queue: "batch-multi");

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
