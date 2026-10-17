using Core.Geo.Events;
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

        _recurringJobs.RemoveIfExists("confirm-nashville-ai-weather-v3");
        _recurringJobs.RemoveIfExists("confirm-nashville-ai-weather-v4");

        // Keep the job in the dashboard for manual triggers, but never schedule it automatically.
        _recurringJobs.AddOrUpdateCQMediatorEvent(
            "import-cities",
            Cron.Never(),
            new ImportCitiesEvent(),
            queue: "batch-single");

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
