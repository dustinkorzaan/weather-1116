namespace Core.AIWeather.Services;

/// <summary>
/// Normalizes Azure AI Foundry project or OpenAI endpoint URLs for ResponsesClient
/// and hosted-agent <c>ProjectOpenAIClient</c> (same <c>/openai/v1</c> suffix as Foundry Console V5).
/// </summary>
public static class FoundryOpenAiEndpoint
{
    private const string OpenAiPathSuffix = "/openai/v1";

    /// <summary>
    /// Returns a URI suitable for <c>ResponsesClientOptions.Endpoint</c> and
    /// <c>ProjectOpenAIClientOptions.Endpoint</c>. Accepts either a project URL
    /// (e.g. <c>.../api/projects/{id}</c>) or an already-resolved OpenAI URL
    /// (e.g. <c>.../openai/v1</c>).
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
    /// Returns the Foundry <strong>project</strong> URI (strips a trailing <c>/openai/v1</c>).
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
