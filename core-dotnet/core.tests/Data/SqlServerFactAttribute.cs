namespace Core.Tests.Data;

/// <summary>
/// A fact that needs a real SQL Server (e.g. geography queries, which the EF InMemory provider
/// can't evaluate in meters). Skipped unless CORE_TESTS_SQL_CONNECTION_STRING is set -- CI's
/// test-core job sets it against its mssql service container.
/// </summary>
public sealed class SqlServerFactAttribute : FactAttribute
{
    public const string ConnectionStringVariable = "CORE_TESTS_SQL_CONNECTION_STRING";

    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            Skip = $"{ConnectionStringVariable} is not set.";
        }
    }

    public static string? ConnectionString => Environment.GetEnvironmentVariable(ConnectionStringVariable);
}
