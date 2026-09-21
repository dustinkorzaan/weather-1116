using System.ClientModel;
using System.ClientModel.Primitives;
using Azure.AI.Extensions.OpenAI;
using OpenAI.Conversations;
using OpenAI.Responses;

namespace Core.AIWeather.Services;

/// <summary>
/// Builds <see cref="ProjectResponsesClient"/> instances for named Foundry prompt agents
/// (Chat3, Current AI Weather V5). Same sequence as Foundry Console V5
/// <c>Program.cs</c>: api-key <see cref="ProjectOpenAIClient"/>, env URL as-is,
/// <c>GetProjectResponsesClientForAgent</c>, then a real project conversation id.
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

        var projectOpenAIClient = new ProjectOpenAIClient(
            ApiKeyAuthenticationPolicy.CreateHeaderApiKeyPolicy(new ApiKeyCredential(apiKey), "api-key"),
            new ProjectOpenAIClientOptions
            {
                Endpoint = endpoint,
            });

        var responseClient = projectOpenAIClient.GetProjectResponsesClientForAgent(agentName);
        var conversation = (await projectOpenAIClient
            .GetProjectConversationsClient()
            .CreateProjectConversationAsync(new ConversationCreationOptions(), cancellationToken)).Value;

        return (responseClient, conversation.Id);
    }
}
