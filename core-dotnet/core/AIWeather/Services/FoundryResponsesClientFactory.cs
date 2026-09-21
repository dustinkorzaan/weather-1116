using System.ClientModel;
using System.ClientModel.Primitives;
using OpenAI.Responses;

namespace Core.AIWeather.Services;

/// <summary>
/// Builds a <see cref="ResponsesClient"/> for direct model inference against a
/// Foundry account endpoint (Chat tabs, Current AI Weather V3/V4). Uses the
/// AZURE_FOUNDRY_PROD_KEY API key when present (FoundryConsoleV1-V5 and any
/// other caller that sets it), otherwise falls back to this app's managed
/// identity via <see cref="FoundryTokenCredentialFactory"/>.
/// </summary>
public static class FoundryResponsesClientFactory
{
    public static ResponsesClient Create(Uri endpoint, string? apiKey)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        var options = new ResponsesClientOptions { Endpoint = endpoint };

        if (!string.IsNullOrEmpty(apiKey))
        {
            return new ResponsesClient(credential: new ApiKeyCredential(apiKey), options: options);
        }

        var tokenPolicy = new BearerTokenPolicy(
            FoundryTokenCredentialFactory.Create(),
            FoundryTokenCredentialFactory.CognitiveServicesScope);

        return new ResponsesClient(authenticationPolicy: tokenPolicy, options: options);
    }
}
