using Hangfire;

namespace WeatherWorkerHangfire;

/// <summary>
/// Registers the worker's recurring jobs once the host starts. Job cadence is
/// intentionally simple for now; it will grow as real work is added.
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
		_recurringJobs.AddOrUpdate<WeatherForecastJob>(
			"weather-forecast",
			job => job.RunAsync(),
			Cron.Minutely);

		return Task.CompletedTask;
	}

	public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
