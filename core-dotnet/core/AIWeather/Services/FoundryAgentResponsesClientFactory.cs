using System.ClientModel;
using System.ClientModel.Primitives;
using Azure.AI.Extensions.OpenAI;
using OpenAI.Conversations;
using OpenAI.Responses;

namespace Core.AIWeather.Services;

/// <summary>
/// Builds <see cref="ProjectResponsesClient"/> instances for Chat3 and Current AI Weather V5, the
/// same shape as Foundry Console V5 <c>Program.cs</c>: api-key auth, <c>GetProjectResponsesClientForAgent</c>,
/// then <c>CreateProjectConversationAsync</c>. This is its own implementation, independent of Console
/// V5 - it does not call it and Console V5 does not call this.
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

        var apiKey = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_KEY")
            ?? throw new InvalidOperationException("Missing AZURE_FOUNDRY_PROD_KEY.");

        var resolvedEndpoint = FoundryOpenAiEndpoint.Resolve(endpoint.ToString());

        var projectOpenAIClient = new ProjectOpenAIClient(
            ApiKeyAuthenticationPolicy.CreateHeaderApiKeyPolicy(new ApiKeyCredential(apiKey), "api-key"),
            new ProjectOpenAIClientOptions
            {
                Endpoint = resolvedEndpoint,
            });

        var responseClient = projectOpenAIClient.GetProjectResponsesClientForAgent(agentName);
        var conversation = (await projectOpenAIClient
            .GetProjectConversationsClient()
            .CreateProjectConversationAsync(new ConversationCreationOptions(), cancellationToken)).Value;

        return (responseClient, conversation.Id);
    }
}
