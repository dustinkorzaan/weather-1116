using Core.Data;
using Microsoft.Data.SqlClient;

namespace Core.Tests.Data;

public class ManagedIdentitySqlConnectionStringFactoryTests
{
    private const string BaseConnectionString =
        "Server=tcp:wx1116-prod-sql-srv.database.windows.net,1433;Initial Catalog=wx1116-prod-sql-database;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30";

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Build_ReturnsNull_WhenBaseConnectionStringIsBlank(string? baseConnectionString)
    {
        Assert.Null(ManagedIdentitySqlConnectionStringFactory.Build(baseConnectionString, "client-id"));
    }

    [Fact]
    public void Build_AddsActiveDirectoryDefaultAuthentication()
    {
        var result = ManagedIdentitySqlConnectionStringFactory.Build(BaseConnectionString, managedIdentityClientId: null);

        var builder = new SqlConnectionStringBuilder(result);
        Assert.Equal(SqlAuthenticationMethod.ActiveDirectoryDefault, builder.Authentication);
        Assert.Equal(string.Empty, builder.UserID);
    }

    [Fact]
    public void Build_SetsUserIdToManagedIdentityClientId_SoDefaultAzureCredentialPicksTheRightIdentity()
    {
        var result = ManagedIdentitySqlConnectionStringFactory.Build(BaseConnectionString, "11111111-1111-1111-1111-111111111111");

        var builder = new SqlConnectionStringBuilder(result);
        Assert.Equal(SqlAuthenticationMethod.ActiveDirectoryDefault, builder.Authentication);
        Assert.Equal("11111111-1111-1111-1111-111111111111", builder.UserID);
    }

    [Fact]
    public void Build_LeavesExplicitAuthenticationModeUntouched()
    {
        var explicitConnectionString = BaseConnectionString + ";Authentication=Active Directory Managed Identity;User ID=22222222-2222-2222-2222-222222222222";

        var result = ManagedIdentitySqlConnectionStringFactory.Build(explicitConnectionString, "11111111-1111-1111-1111-111111111111");

        var builder = new SqlConnectionStringBuilder(result);
        Assert.Equal(SqlAuthenticationMethod.ActiveDirectoryManagedIdentity, builder.Authentication);
        Assert.Equal("22222222-2222-2222-2222-222222222222", builder.UserID);
    }
}
