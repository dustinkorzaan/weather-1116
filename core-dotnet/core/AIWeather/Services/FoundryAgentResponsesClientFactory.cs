using Azure.AI.Extensions.OpenAI;
using OpenAI.Conversations;
using OpenAI.Responses;

namespace Core.AIWeather.Services;

/// <summary>
/// Builds <see cref="ProjectResponsesClient"/> instances for Chat3 and Current AI Weather V5:
/// <c>GetProjectResponsesClientForAgent</c>, then <c>CreateProjectConversationAsync</c>. Always
/// authenticates via this app's managed identity (<see cref="FoundryTokenCredentialFactory"/>) -
/// no API key. This is its own implementation, independent of Console V5's own
/// <c>Program.cs</c> (which builds its client directly with AZURE_FOUNDRY_PROD_KEY) - neither
/// calls the other.
/// </summary>
/// <remarks>
/// Unlike Console V5, the endpoint is resolved through <see cref="FoundryOpenAiEndpoint.Resolve"/>,
/// which appends <c>/openai/v1</c> to <c>AZURE_FOUNDRY_PROD_PROJ_URL</c> when it's not already there.
/// Passing the raw project URL (no <c>/openai/v1</c>) resolves both calls to an Azure-classic path
/// that requires an explicit <c>api-version</c> query parameter neither call sends, and
/// <c>CreateResponseStreamingAsync</c> fails with "Missing required query parameter: api-version".
/// </remarks>
public static class FoundryAgentResponsesClientFactory
{
    public static Task<(ProjectResponsesClient ResponseClient, string ConversationId)> CreateForAgentAsync(
        string agentName,
        CancellationToken cancellationToken = default)
    {
        var projectUrl = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_PROJ_URL")
            ?? throw new InvalidOperationException("Missing AZURE_FOUNDRY_PROD_PROJ_URL.");

        return CreateForAgentAsync(agentName, new Uri(projectUrl), cancellationToken);
    }

    public static async Task<(ProjectResponsesClient ResponseClient, string ConversationId)> CreateForAgentAsync(
        string agentName,
        Uri endpoint,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentName);
        ArgumentNullException.ThrowIfNull(endpoint);

        var resolvedEndpoint = FoundryOpenAiEndpoint.Resolve(endpoint.ToString());

        var projectOpenAIClient = new ProjectOpenAIClient(
            resolvedEndpoint,
            FoundryTokenCredentialFactory.Create(),
            new ProjectOpenAIClientOptions());

        var responseClient = projectOpenAIClient.GetProjectResponsesClientForAgent(agentName);
        var conversation = (await projectOpenAIClient
            .GetProjectConversationsClient()
            .CreateProjectConversationAsync(new ConversationCreationOptions(), cancellationToken)).Value;

        return (responseClient, conversation.Id);
    }
}
