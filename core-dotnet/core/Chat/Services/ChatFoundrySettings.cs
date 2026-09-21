using Azure.AI.Extensions.OpenAI;
using Core.AIWeather.Services;
using OpenAI.Responses;

namespace Core.Chat.Services;

public sealed class ChatFoundrySettings
{
    public string Endpoint { get; }

    /// <summary>
    /// Foundry API key (AZURE_FOUNDRY_PROD_KEY). Only the FoundryConsoleV1-V5
    /// dev-tool consoles set this; hosted services (API/MVC/Worker) leave it
    /// unset and authenticate with this app's managed identity instead, via
    /// <see cref="FoundryTokenCredentialFactory"/>.
    /// </summary>
    public string? ApiKey { get; }

    public string DeploymentName { get; }

    private readonly string? _chatAgentName;

    public ChatFoundrySettings()
    {
        Endpoint = FoundryOpenAiEndpoint.Resolve(
            Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_PROJ_URL")
            ?? throw new InvalidOperationException("Missing AZURE_FOUNDRY_PROD_PROJ_URL.")).ToString();

        ApiKey = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_KEY");

        DeploymentName = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_MODEL")
            ?? throw new InvalidOperationException("Missing AZURE_FOUNDRY_PROD_MODEL.");

        var chatAgentName = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_CHAT_AGENT_NAME");
        _chatAgentName = string.IsNullOrWhiteSpace(chatAgentName) ? null : chatAgentName.Trim();
    }

    public string ChatAgentName =>
        _chatAgentName
        ?? throw new InvalidOperationException("Missing AZURE_FOUNDRY_PROD_CHAT_AGENT_NAME.");

    public ResponsesClient CreateResponsesClient() =>
        FoundryResponsesClientFactory.Create(new Uri(Endpoint), ApiKey);

    /// <summary>
    /// Responses client bound to the hosted Foundry agent. Chat3 sends only the
    /// user prompt; instructions, tools, model, and MCP approval live on the agent.
    /// Same client + conversation sequence as Foundry Console V5.
    /// </summary>
    public Task<(ProjectResponsesClient ResponseClient, string ConversationId)> CreateProjectResponsesClientForChatAgentAsync(
        CancellationToken cancellationToken = default) =>
        FoundryAgentResponsesClientFactory.CreateForAgentAsync(ChatAgentName, cancellationToken);
}
