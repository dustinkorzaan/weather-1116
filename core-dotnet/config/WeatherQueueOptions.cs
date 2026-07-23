namespace Core.config;

/// <summary>
/// Shared, strongly-typed queue settings bound from flat root configuration keys
/// (no nesting). The key constants are the single source of truth for those keys,
/// reused by worker-queue today and by the API and MVC apps later.
/// </summary>
public class WeatherQueueOptions
{
	public const string ServiceBusConnectionStringKey = "SERVICE_BUS_CONNECTION_STRING";
	public const string DbConnectionStringKey = "DB_CONNECTION_STRING";
	public const string RequestQueueNameKey = "WEATHER_REQUEST_QUEUE_NAME";
	public const string ResponseQueueNameKey = "WEATHER_RESPONSE_QUEUE_NAME";

	/// <summary>Azure Service Bus connection string, or blank to run idle for now.</summary>
	public string? ServiceBusConnectionString { get; set; }

	/// <summary>Database connection string (used once persistence is wired up).</summary>
	public string? DbConnectionString { get; set; }

	/// <summary>Queue the worker reads incoming requests from.</summary>
	public string RequestQueueName { get; set; } = "request";

	/// <summary>Queue the worker writes responses to.</summary>
	public string ResponseQueueName { get; set; } = "response";
}
