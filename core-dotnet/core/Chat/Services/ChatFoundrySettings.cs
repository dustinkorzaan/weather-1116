using System.ClientModel;
using Core.AIWeather.Services;
using OpenAI;
using OpenAI.Responses;

namespace Core.Chat.Services;

public sealed class ChatFoundrySettings
{
    public string Endpoint { get; }
    public string ApiKey { get; }
    public string DeploymentName { get; }

    public ChatFoundrySettings()
    {
        Endpoint = FoundryOpenAiEndpoint.Resolve(
            Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_PROJ_URL")
            ?? throw new InvalidOperationException("Missing AZURE_FOUNDRY_PROD_EUS2_PROJ_URL.")).ToString();

        ApiKey = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_KEY")
            ?? throw new InvalidOperationException("Missing AZURE_FOUNDRY_PROD_EUS2_KEY.");

        DeploymentName = Environment.GetEnvironmentVariable("AZURE_FOUNDRY_PROD_EUS2_MODEL")
            ?? throw new InvalidOperationException("Missing AZURE_FOUNDRY_PROD_EUS2_MODEL.");
    }

    public ResponsesClient CreateResponsesClient() => new(
        credential: new ApiKeyCredential(ApiKey),
        options: new OpenAIClientOptions
        {
            Endpoint = new Uri(Endpoint),
        });
}
