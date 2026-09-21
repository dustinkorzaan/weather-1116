using System.ClientModel;
using System.ClientModel.Primitives;
using Azure.AI.Extensions.OpenAI;
using OpenAI.Conversations;
using OpenAI.Responses;

namespace Core.AIWeather.Services;

/// <summary>
/// Builds <see cref="ProjectResponsesClient"/> instances the same way Foundry Console V5
/// <c>Program.cs</c> does: api-key auth, <c>AZURE_FOUNDRY_PROD_PROJ_URL</c> as-is,
/// <c>GetProjectResponsesClientForAgent</c>, then <c>CreateProjectConversationAsync</c>.
/// <c>FoundryConsoleV5/Program.cs</c> calls this method directly (rather than duplicating the
/// sequence) so Console V5, Chat3, and Current AI Weather V5 can never drift apart.
/// </summary>
/// <remarks>
/// Uses two <see cref="ProjectOpenAIClient"/> instances, not one, because the two calls need opposite
/// "api-version" behavior on a live Foundry project: the agent-scoped responses endpoint requires it
/// (without it, <c>CreateResponseStreamingAsync</c> fails with "Missing required query parameter:
/// api-version"), while the "/v1"-style conversations endpoint rejects it ("api-version query
/// parameter is not allowed when using /v1 path"). <c>ProjectOpenAIClient.CreatePipeline</c> (Azure.AI
/// .Extensions.OpenAI 3.0.0-beta.2) attaches "api-version" to every request on a client's pipeline
/// once, only when <see cref="ProjectOpenAIClientOptions.AgentName"/> is set - there is no per-call
/// override - so getting both behaviors right means building the responses client from an instance
/// with <c>AgentName</c> set and the conversations client from a separate instance without it.
/// </remarks>
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

        var apiKeyPolicy = ApiKeyAuthenticationPolicy.CreateHeaderApiKeyPolicy(new ApiKeyCredential(apiKey), "api-key");

        var responsesHost = new ProjectOpenAIClient(
            apiKeyPolicy,
            new ProjectOpenAIClientOptions
            {
                Endpoint = endpoint,
                AgentName = agentName,
            });
        var responseClient = responsesHost.GetProjectResponsesClientForAgent(agentName);

        var conversationsHost = new ProjectOpenAIClient(
            apiKeyPolicy,
            new ProjectOpenAIClientOptions
            {
                Endpoint = endpoint,
            });
        var conversation = (await conversationsHost
            .GetProjectConversationsClient()
            .CreateProjectConversationAsync(new ConversationCreationOptions(), cancellationToken)).Value;

        return (responseClient, conversation.Id);
    }
}
