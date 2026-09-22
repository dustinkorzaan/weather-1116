using Core.AIWeather.Services;

namespace Core.Tests.AIWeather.Services;

public class FoundryTokenCredentialFactoryTests
{
    // Asserted by GetType().Name rather than Assert.IsType<T>(): the concrete credential
    // types are ambiguous at compile time in this project's reference graph (Azure.Core's
    // preview dependency vendors its own copies alongside the real Azure.Identity package),
    // which is exactly why the production code needs the extern alias in
    // FoundryTokenCredentialFactory.cs. Comparing the runtime type name sidesteps that
    // without pulling the alias into the test project too.

    [Fact]
    public void Resolve_ReturnsManagedIdentityCredential_WhenClientIdSet()
    {
        var credential = FoundryTokenCredentialFactory.Resolve("test-managed-identity-client-id");

        Assert.Equal("ManagedIdentityCredential", credential.GetType().Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Resolve_ReturnsDefaultAzureCredential_WhenClientIdUnset(string? clientId)
    {
        var credential = FoundryTokenCredentialFactory.Resolve(clientId);

        Assert.Equal("DefaultAzureCredential", credential.GetType().Name);
    }
}
