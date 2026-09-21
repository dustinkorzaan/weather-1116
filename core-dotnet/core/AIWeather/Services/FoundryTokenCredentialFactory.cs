extern alias AzureIdentityExplicit;

using Azure.Core;
using AzureIdentity = AzureIdentityExplicit::Azure.Identity;

namespace Core.AIWeather.Services;

/// <summary>
/// Resolves the passwordless credential used for Foundry access when no
/// AZURE_FOUNDRY_PROD_KEY is configured: this app's user-assigned managed
/// identity in Azure (AZURE_CLIENT_ID, set per app by
/// infra/modules/container-app.bicep, the same variable
/// <see cref="Data.ManagedIdentitySqlConnectionStringFactory"/> uses for SQL),
/// or DefaultAzureCredential locally (developer sign-in). TokenCredential
/// derives from System.ClientModel.AuthenticationTokenProvider, so instances
/// from here can be handed directly to the Foundry SDK clients, or wrapped in
/// a BearerTokenPolicy for clients that only accept an AuthenticationPolicy.
/// </summary>
public static class FoundryTokenCredentialFactory
{
    /// <summary>Scope for direct model inference (chat/responses) against the Foundry account.</summary>
    public const string CognitiveServicesScope = "https://cognitiveservices.azure.com/.default";

    public static TokenCredential Create()
    {
        var clientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");

        return string.IsNullOrWhiteSpace(clientId)
            ? new AzureIdentity.DefaultAzureCredential()
            : new AzureIdentity.ManagedIdentityCredential(AzureIdentity.ManagedIdentityId.FromUserAssignedClientId(clientId));
    }
}
