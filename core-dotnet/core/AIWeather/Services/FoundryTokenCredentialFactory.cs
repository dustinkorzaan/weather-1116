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
    /// <summary>
    /// Entra audience for Foundry project endpoints (<c>*.services.ai.azure.com/api/projects/...</c>),
    /// used by both direct model inference (<see cref="FoundryResponsesClientFactory"/>) and
    /// hosted-agent calls (<see cref="FoundryAgentResponsesClientFactory"/>). A
    /// <c>cognitiveservices.azure.com</c> token 401s against the project OpenAI path.
    /// </summary>
    public const string FoundryScope = "https://ai.azure.com/.default";

    // Cached rather than rebuilt per call: DefaultAzureCredential/ManagedIdentityCredential
    // each hold their own token cache internally, so reusing the same instance across
    // requests lets that cache actually do its job instead of forcing a fresh AAD/IMDS
    // token request on every single Foundry call.
    private static readonly Lazy<TokenCredential> Cached =
        new(() => Resolve(Environment.GetEnvironmentVariable("AZURE_CLIENT_ID")));

    public static TokenCredential Create() => Cached.Value;

    /// <summary>
    /// The type-selection logic, kept separate from <see cref="Cached"/> so tests can exercise
    /// both branches directly -- Create() itself is a process-wide singleton once evaluated, so
    /// toggling AZURE_CLIENT_ID and re-calling it would only ever observe the first branch taken.
    /// </summary>
    internal static TokenCredential Resolve(string? managedIdentityClientId) =>
        string.IsNullOrWhiteSpace(managedIdentityClientId)
            ? new AzureIdentity.DefaultAzureCredential()
            : new AzureIdentity.ManagedIdentityCredential(AzureIdentity.ManagedIdentityId.FromUserAssignedClientId(managedIdentityClientId));
}
