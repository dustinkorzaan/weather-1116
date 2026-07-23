namespace WeatherWorkerQueue;

/// <summary>
/// Queue settings (Azure Service Bus). The connection string / namespace and any
/// keys are supplied later; until then the worker starts but stays idle.
/// </summary>
public class QueueOptions
{
	public const string SectionName = "Queue";

	/// <summary>Full connection string, or blank to run in idle mode for now.</summary>
	public string? ConnectionString { get; set; }

	/// <summary>Queue the worker listens on.</summary>
	public string QueueName { get; set; } = "weather-jobs";
}
