using System.ClientModel;
using System.ClientModel.Primitives;
using Azure.AI.Extensions.OpenAI;
using Core.AIWeather.Services;
using OpenAI;
using OpenAI.Responses;

namespace Core.Chat.Services;

public sealed class ChatFoundrySettings
{
    public string Endpoint { get; }
    public string ApiKey { get; }
    public string DeploymentName { get; }

    private readonly string? _chatAgentName;

    public ChatFoundrySettings()
    {
        Endpoint = FoundryOpenAiEndpoint.Resolve(
            Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_PROJ_URL")
            ?? throw new InvalidOperationException("Missing AZURE_FOUNDRY_PROD_EUS2_PROJ_URL.")).ToString();

        ApiKey = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_KEY")
            ?? throw new InvalidOperationException("Missing AZURE_FOUNDRY_PROD_EUS2_KEY.");

        DeploymentName = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_MODEL")
            ?? throw new InvalidOperationException("Missing AZURE_FOUNDRY_PROD_EUS2_MODEL.");

        var chatAgentName = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_CHAT_AGENT_NAME");
        _chatAgentName = string.IsNullOrWhiteSpace(chatAgentName) ? null : chatAgentName.Trim();
    }

    public string ChatAgentName =>
        _chatAgentName
        ?? throw new InvalidOperationException("Missing AZURE_FOUNDRY_PROD_EUS2_CHAT_AGENT_NAME.");

    public ResponsesClient CreateResponsesClient() => new(
        credential: new ApiKeyCredential(ApiKey),
        options: new OpenAIClientOptions
        {
            Endpoint = new Uri(Endpoint),
        });

    /// <summary>
    /// Responses client bound to the hosted Foundry agent. Chat3 sends only the
    /// user prompt; instructions, tools, model, and MCP approval live on the agent.
    /// </summary>
    public ProjectResponsesClient CreateProjectResponsesClientForChatAgent()
    {
        ProjectOpenAIClient projectOpenAIClient = new(
            ApiKeyAuthenticationPolicy.CreateHeaderApiKeyPolicy(new ApiKeyCredential(ApiKey), "api-key"),
            new ProjectOpenAIClientOptions
            {
                Endpoint = new Uri(Endpoint),
            });

        return projectOpenAIClient.GetProjectResponsesClientForAgent(ChatAgentName);
    }
}
