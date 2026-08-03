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
        _recurringJobs.AddOrUpdateMediatREvent<ConfirmNashvilleAIWeatherEvent>(
            "confirm-nashville-ai-weather",
            Cron.Daily(2),
            queue: "batch-multi");

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
