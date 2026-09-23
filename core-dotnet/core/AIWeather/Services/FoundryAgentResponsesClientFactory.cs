using Azure.AI.Extensions.OpenAI;
using OpenAI.Conversations;
using OpenAI.Responses;

namespace Core.AIWeather.Services;

/// <summary>
/// Builds <see cref="ProjectResponsesClient"/> instances for Chat3 and Current AI Weather V5:
/// <c>GetProjectResponsesClientForAgent</c>, then <c>CreateProjectConversationAsync</c> (skipped
/// when the caller passes an existing conversation id to continue, as Chat3 does on later turns). Always
/// authenticates via this app's managed identity (<see cref="FoundryTokenCredentialFactory"/>) -
/// no API key. This is its own implementation, independent of Console V5's own
/// <c>Program.cs</c> (which builds its client directly with AZURE_FOUNDRY_PROD_KEY) - neither
/// calls the other.
/// </summary>
/// <remarks>
/// The TokenCredential constructor takes the Azure AI <em>project</em> endpoint and appends
/// <c>/openai/v1</c> itself. Passing an already-resolved <c>.../openai/v1</c> URL (what
/// <see cref="FoundryOpenAiEndpoint.Resolve"/> produces for <c>ResponsesClient</c>) double-appends
/// the suffix and <c>CreateProjectConversationAsync</c> 404s against
/// <c>.../openai/v1/openai/v1/conversations</c>. Console V5's api-key constructor is different:
/// it sets <c>ProjectOpenAIClientOptions.Endpoint</c> as the request base URI and therefore
/// still needs <see cref="FoundryOpenAiEndpoint.Resolve"/>.
/// </remarks>
public static class FoundryAgentResponsesClientFactory
{
    public static Task<(ProjectResponsesClient ResponseClient, string ConversationId)> CreateForAgentAsync(
        string agentName,
        CancellationToken cancellationToken = default) =>
        CreateForAgentAsync(agentName, existingConversationId: null, cancellationToken);

    public static Task<(ProjectResponsesClient ResponseClient, string ConversationId)> CreateForAgentAsync(
        string agentName,
        string? existingConversationId,
        CancellationToken cancellationToken = default)
    {
        var projectUrl = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_PROJ_URL")
            ?? throw new InvalidOperationException("Missing AZURE_FOUNDRY_PROD_PROJ_URL.");

        return CreateForAgentAsync(agentName, new Uri(projectUrl), existingConversationId, cancellationToken);
    }

    public static Task<(ProjectResponsesClient ResponseClient, string ConversationId)> CreateForAgentAsync(
        string agentName,
        Uri endpoint,
        CancellationToken cancellationToken = default) =>
        CreateForAgentAsync(agentName, endpoint, existingConversationId: null, cancellationToken);

    public static async Task<(ProjectResponsesClient ResponseClient, string ConversationId)> CreateForAgentAsync(
        string agentName,
        Uri endpoint,
        string? existingConversationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentName);
        ArgumentNullException.ThrowIfNull(endpoint);

        var projectEndpoint = FoundryOpenAiEndpoint.ResolveProjectEndpoint(endpoint.ToString());

        // Confirmed empirically (capturing TokenRequestContext.Scopes from a test credential):
        // this ctor overload requests https://ai.azure.com/.default, the same audience
        // FoundryResponsesClientFactory, prod-deploy-foundry-agents.yml, and the
        // Wx1116GeoNonAIWeather toolbox connection use. Foundry User at project scope
        // (ai-foundry.bicep) is the right role for this.
        var projectOpenAIClient = new ProjectOpenAIClient(
            projectEndpoint,
            FoundryTokenCredentialFactory.Create(),
            new ProjectOpenAIClientOptions());

        var responseClient = projectOpenAIClient.GetProjectResponsesClientForAgent(agentName);
        if (!string.IsNullOrWhiteSpace(existingConversationId))
        {
            return (responseClient, existingConversationId);
        }

        var conversation = (await projectOpenAIClient
            .GetProjectConversationsClient()
            .CreateProjectConversationAsync(new ConversationCreationOptions(), cancellationToken)).Value;

        return (responseClient, conversation.Id);
    }
}
