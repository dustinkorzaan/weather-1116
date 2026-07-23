using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.config;

/// <summary>
/// Registration helpers for <see cref="WeatherQueueOptions"/>.
/// </summary>
public static class QueueConfigurationExtensions
{
	/// <summary>
	/// Binds <see cref="WeatherQueueOptions"/> from flat root configuration keys so
	/// worker-queue, the API, and MVC all resolve queue settings identically.
	/// Missing queue names fall back to the option defaults.
	/// </summary>
	public static IServiceCollection AddWeatherQueueOptions(
		this IServiceCollection services,
		IConfiguration configuration)
	{
		services.Configure<WeatherQueueOptions>(options =>
		{
			options.ConnectionString = configuration[WeatherQueueOptions.ConnectionStringKey];
			options.RequestQueueName =
				configuration[WeatherQueueOptions.RequestQueueNameKey] ?? options.RequestQueueName;
			options.ResponseQueueName =
				configuration[WeatherQueueOptions.ResponseQueueNameKey] ?? options.ResponseQueueName;
		});

		return services;
	}
}
