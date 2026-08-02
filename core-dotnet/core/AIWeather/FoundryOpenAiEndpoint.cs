namespace Core.AIWeather;

/// <summary>
/// Normalizes Azure AI Foundry project or OpenAI endpoint URLs for ResponsesClient.
/// </summary>
public static class FoundryOpenAiEndpoint
{
	private const string OpenAiPathSuffix = "/openai/v1";

	/// <summary>
	/// Returns a URI suitable for <c>OpenAIClientOptions.Endpoint</c> or
	/// <c>ProjectOpenAIClientOptions.Endpoint</c>.
	/// Accepts either a project URL (e.g. <c>.../api/projects/{id}</c>) or an
	/// already-resolved OpenAI URL (e.g. <c>.../openai/v1</c>).
	/// </summary>
	public static Uri Resolve(string projectOrEndpointUrl)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(projectOrEndpointUrl);

		var trimmed = projectOrEndpointUrl.TrimEnd('/');
		if (trimmed.EndsWith(OpenAiPathSuffix, StringComparison.OrdinalIgnoreCase))
		{
			return new Uri(trimmed);
		}

		return new Uri($"{trimmed}{OpenAiPathSuffix}");
	}
}
