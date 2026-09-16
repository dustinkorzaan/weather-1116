using Microsoft.Data.SqlClient;

namespace Core.Data;

/// <summary>
/// Builds Azure SQL connection strings that authenticate via Entra ID
/// (Microsoft.Data.SqlClient's "Active Directory Default" mode, which
/// resolves a token the same way <c>DefaultAzureCredential</c> does --
/// user-assigned managed identity in Azure, developer credentials locally)
/// instead of a SQL login/password baked into the string.
///
/// The auth clause lives in the connection string itself, so every consumer
/// that opens a <see cref="SqlConnection"/> from it authenticates the same
/// way with no driver-specific code of its own -- Hangfire's SqlServerStorage
/// and an Entity Framework Core DbContext (<c>UseSqlServer</c>) both included.
/// </summary>
public static class ManagedIdentitySqlConnectionStringFactory
{
    /// <param name="baseConnectionString">
    /// Server/database portion of the connection string (e.g. from
    /// DB_CONNECTION_STRING). Null/blank returns null so callers can keep
    /// falling back to in-memory storage locally.
    /// </param>
    /// <param name="managedIdentityClientId">
    /// Client ID of this app's user-assigned managed identity (AZURE_CLIENT_ID,
    /// set per app by infra/modules/container-app.bicep). Passed through as
    /// the connection string's User ID so DefaultAzureCredential knows which
    /// identity to use instead of guessing among several.
    /// </param>
    public static string? Build(string? baseConnectionString, string? managedIdentityClientId)
    {
        if (string.IsNullOrWhiteSpace(baseConnectionString))
        {
            return null;
        }

        var builder = new SqlConnectionStringBuilder(baseConnectionString);

        // An explicit Authentication mode already in the string (e.g. a SQL
        // login/password override for local testing against a real Azure SQL
        // instance) is left alone rather than forced into Entra auth.
        if (builder.Authentication != SqlAuthenticationMethod.NotSpecified)
        {
            return builder.ConnectionString;
        }

        builder.Authentication = SqlAuthenticationMethod.ActiveDirectoryDefault;

        if (!string.IsNullOrWhiteSpace(managedIdentityClientId))
        {
            builder.UserID = managedIdentityClientId;
        }

        return builder.ConnectionString;
    }
}
