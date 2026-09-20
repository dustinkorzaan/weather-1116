namespace Core.AIWeather.Services;

/// <summary>
/// Normalizes Azure AI Foundry project or OpenAI endpoint URLs for ResponsesClient.
/// </summary>
public static class FoundryOpenAiEndpoint
{
    private const string OpenAiPathSuffix = "/openai/v1";

    /// <summary>
    /// Returns a URI suitable for <c>ResponsesClientOptions.Endpoint</c> (model-direct V3/V4 calls).
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

    /// <summary>
    /// Returns the Foundry <strong>project</strong> URI for <see cref="ProjectOpenAIClient"/>
    /// and hosted-agent Responses calls (Chat3, V5). Strips a trailing <c>/openai/v1</c> when
    /// present so callers can reuse <c>AZURE_FOUNDRY_PROD_PROJ_URL</c> whether or not it already
    /// includes the inference suffix (see Foundry Console V5).
    /// </summary>
    public static Uri ResolveProjectEndpoint(string projectOrEndpointUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectOrEndpointUrl);

        var trimmed = projectOrEndpointUrl.TrimEnd('/');
        if (trimmed.EndsWith(OpenAiPathSuffix, StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[..^OpenAiPathSuffix.Length];
        }

        return new Uri(trimmed);
    }
}
