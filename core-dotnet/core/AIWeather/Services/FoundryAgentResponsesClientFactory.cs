using System.ClientModel;
using System.ClientModel.Primitives;
using Azure.AI.Extensions.OpenAI;
using OpenAI.Conversations;
using OpenAI.Responses;

namespace Core.AIWeather.Services;

/// <summary>
/// Builds <see cref="ProjectResponsesClient"/> instances the same way Foundry Console V5
/// <c>Program.cs</c> does: api-key auth, <c>AZURE_FOUNDRY_PROD_PROJ_URL</c> as-is,
/// <c>GetProjectResponsesClientForAgent</c>, then <c>CreateProjectConversationAsync</c>. Also sets
/// <c>ProjectOpenAIClientOptions.AgentName</c>, which Console V5 does not - required so the SDK
/// attaches "api-version" to every request (see comment below).
/// </summary>
public static class FoundryAgentResponsesClientFactory
{
    public static Task<(ProjectResponsesClient ResponseClient, string ConversationId)> CreateForAgentAsync(
        string agentName,
        CancellationToken cancellationToken = default)
    {
        var endpoint = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_PROJ_URL")
            ?? throw new InvalidOperationException("Missing AZURE_FOUNDRY_PROD_PROJ_URL.");

        return CreateForAgentAsync(agentName, new Uri(endpoint), cancellationToken);
    }

    public static async Task<(ProjectResponsesClient ResponseClient, string ConversationId)> CreateForAgentAsync(
        string agentName,
        Uri endpoint,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentName);
        ArgumentNullException.ThrowIfNull(endpoint);

        var apiKey = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_KEY")
            ?? throw new InvalidOperationException("Missing AZURE_FOUNDRY_PROD_KEY.");

        // ProjectOpenAIClient.CreatePipeline (Azure.AI.Extensions.OpenAI 3.0.0-beta.2) only adds the
        // "api-version" query parameter to the pipeline when ProjectOpenAIClientOptions.AgentName is
        // set. Without it, every request through this client (conversations, responses, ...) omits
        // api-version and Foundry rejects it with "Missing required query parameter: api-version".
        // Confirmed by capturing the raw outgoing request URI against a local test listener: with
        // AgentName unset, CreateProjectConversationAsync sent "POST .../conversations" (no query
        // string); with it set, the same call sent "POST .../conversations?api-version=v1". Setting it
        // here does not change which endpoint is called or switch auth - it only flips this internal
        // pipeline check.
        var projectOpenAIClient = new ProjectOpenAIClient(
            ApiKeyAuthenticationPolicy.CreateHeaderApiKeyPolicy(new ApiKeyCredential(apiKey), "api-key"),
            new ProjectOpenAIClientOptions
            {
                Endpoint = endpoint,
                AgentName = agentName,
            });

        var responseClient = projectOpenAIClient.GetProjectResponsesClientForAgent(agentName);
        var conversation = (await projectOpenAIClient
            .GetProjectConversationsClient()
            .CreateProjectConversationAsync(new ConversationCreationOptions(), cancellationToken)).Value;

        return (responseClient, conversation.Id);
    }
}
