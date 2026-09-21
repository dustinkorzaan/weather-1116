using System.ClientModel.Primitives;
using OpenAI.Responses;

namespace Core.AIWeather.Services;

/// <summary>
/// Builds a <see cref="ResponsesClient"/> for direct model inference against a
/// Foundry account endpoint (Chat tabs, Current AI Weather V3/V4). Always
/// authenticates via this app's managed identity (<see cref="FoundryTokenCredentialFactory"/>) -
/// no API key. Only the FoundryConsoleV1-V5 dev-tool consoles build their own
/// clients directly with AZURE_FOUNDRY_PROD_KEY; they do not call this.
/// </summary>
public static class FoundryResponsesClientFactory
{
    public static ResponsesClient Create(Uri endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        var tokenPolicy = new BearerTokenPolicy(
            FoundryTokenCredentialFactory.Create(),
            FoundryTokenCredentialFactory.CognitiveServicesScope);

        return new ResponsesClient(
            authenticationPolicy: tokenPolicy,
            options: new ResponsesClientOptions { Endpoint = endpoint });
    }
}
