using System.ClientModel;
using System.ClientModel.Primitives;
using Azure.AI.Extensions.OpenAI;
using OpenAI.Responses;

namespace Core.AIWeather.Services;

/// <summary>
/// Builds <see cref="ProjectResponsesClient"/> instances for named Foundry prompt agents
/// (Chat3, Current AI Weather V5, Foundry Console V5).
/// </summary>
public static class FoundryAgentResponsesClientFactory
{
    public static ProjectResponsesClient CreateForAgent(string agentName) =>
        CreateForAgent(agentName, ResolveProjectEndpointFromEnvironment());

    /// <param name="projectEndpoint">
    /// Foundry project URI (not the <c>/openai/v1</c> inference suffix). When omitted, resolved
    /// from <c>AZURE_FOUNDRY_PROD_PROJ_URL</c>.
    /// </param>
    public static ProjectResponsesClient CreateForAgent(string agentName, Uri projectEndpoint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentName);
        ArgumentNullException.ThrowIfNull(projectEndpoint);

        var apiKey = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_KEY")
            ?? throw new InvalidOperationException("Missing AZURE_FOUNDRY_PROD_KEY.");

        ProjectOpenAIClient projectOpenAIClient = new(
            ApiKeyAuthenticationPolicy.CreateHeaderApiKeyPolicy(new ApiKeyCredential(apiKey), "api-key"),
            new ProjectOpenAIClientOptions
            {
                Endpoint = projectEndpoint,
            });

        return projectOpenAIClient.GetProjectResponsesClientForAgent(agentName);
    }

    public static Uri ResolveProjectEndpointFromEnvironment() =>
        FoundryOpenAiEndpoint.ResolveProjectEndpoint(
            Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_PROJ_URL")
            ?? throw new InvalidOperationException("Missing AZURE_FOUNDRY_PROD_PROJ_URL."));
}
