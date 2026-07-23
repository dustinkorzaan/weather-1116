namespace WeatherWorkerQueue;

/// <summary>
/// Queue settings (Azure Service Bus), bound from flat root configuration keys.
/// The connection string / namespace and any keys are supplied later; until then
/// the worker starts but stays idle.
/// </summary>
public class QueueOptions
{
	/// <summary>Full connection string, or blank to run in idle mode for now.</summary>
	public string? ConnectionString { get; set; }

	/// <summary>Queue the worker reads incoming requests from.</summary>
	public string RequestQueueName { get; set; } = "request";

	/// <summary>Queue the worker writes responses to.</summary>
	public string ResponseQueueName { get; set; } = "response";
}
