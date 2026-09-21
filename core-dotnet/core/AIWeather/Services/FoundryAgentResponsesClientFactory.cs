using System.ClientModel;
using System.ClientModel.Primitives;
using Azure.AI.Extensions.OpenAI;
using OpenAI.Responses;

namespace Core.AIWeather.Services;

/// <summary>
/// Builds <see cref="ProjectResponsesClient"/> instances for named Foundry prompt agents
/// (Chat3, Current AI Weather V5). Matches Foundry Console V5: api-key auth, <c>/openai/v1</c>
/// endpoint, <c>GetProjectResponsesClientForAgent</c>, user prompt only.
/// </summary>
public static class FoundryAgentResponsesClientFactory
{
    public static ProjectResponsesClient CreateForAgent(string agentName) =>
        CreateForAgent(agentName, ResolveEndpointFromEnvironment());

    /// <param name="endpoint">
    /// Foundry OpenAI endpoint (<c>.../openai/v1</c>). When omitted, resolved from
    /// <c>AZURE_FOUNDRY_PROD_PROJ_URL</c> via <see cref="FoundryOpenAiEndpoint.Resolve"/>.
    /// </param>
    public static ProjectResponsesClient CreateForAgent(string agentName, Uri endpoint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentName);
        ArgumentNullException.ThrowIfNull(endpoint);

        var apiKey = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_KEY")
            ?? throw new InvalidOperationException("Missing AZURE_FOUNDRY_PROD_KEY.");

        ProjectOpenAIClient projectOpenAIClient = new(
            ApiKeyAuthenticationPolicy.CreateHeaderApiKeyPolicy(new ApiKeyCredential(apiKey), "api-key"),
            new ProjectOpenAIClientOptions
            {
                Endpoint = endpoint,
            });

        return projectOpenAIClient.GetProjectResponsesClientForAgent(agentName);
    }

    public static Uri ResolveEndpointFromEnvironment() =>
        FoundryOpenAiEndpoint.Resolve(
            Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_PROJ_URL")
            ?? throw new InvalidOperationException("Missing AZURE_FOUNDRY_PROD_PROJ_URL."));
}
