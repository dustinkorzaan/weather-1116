extern alias AzureIdentityExplicit;

using Azure.Core;
using AzureIdentity = AzureIdentityExplicit::Azure.Identity;

namespace Core.AIWeather.Services;

/// <summary>
/// Resolves the passwordless credential API/MVC/Worker use for Foundry access
/// (no AZURE_FOUNDRY_PROD_KEY): this app's user-assigned managed identity in
/// Azure (AZURE_CLIENT_ID, set per app by infra/modules/container-app.bicep,
/// the same variable <see cref="Data.ManagedIdentitySqlConnectionStringFactory"/>
/// uses for SQL), or DefaultAzureCredential locally (developer sign-in).
/// TokenCredential derives from System.ClientModel.AuthenticationTokenProvider,
/// so instances from here can be handed directly to the Foundry SDK clients,
/// or wrapped in a BearerTokenPolicy for clients that only accept an
/// AuthenticationPolicy.
/// </summary>
public static class FoundryTokenCredentialFactory
{
    /// <summary>Scope for direct model inference (chat/responses) against the Foundry account.</summary>
    public const string CognitiveServicesScope = "https://cognitiveservices.azure.com/.default";

    // Cached rather than rebuilt per call: DefaultAzureCredential/ManagedIdentityCredential
    // each hold their own token cache internally, so reusing the same instance across
    // requests lets that cache actually do its job instead of forcing a fresh AAD/IMDS
    // token request on every single Foundry call.
    private static readonly Lazy<TokenCredential> Cached = new(CreateCore);

    public static TokenCredential Create() => Cached.Value;

    private static TokenCredential CreateCore()
    {
        var clientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");

        return string.IsNullOrWhiteSpace(clientId)
            ? new AzureIdentity.DefaultAzureCredential()
            : new AzureIdentity.ManagedIdentityCredential(AzureIdentity.ManagedIdentityId.FromUserAssignedClientId(clientId));
    }
}
