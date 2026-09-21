using System.ClientModel;
using System.ClientModel.Primitives;
using System.Net;
using Azure.AI.Extensions.OpenAI;
using Azure.Core;
using Core.AIWeather.Services;
using OpenAI.Conversations;

namespace Core.Tests.AIWeather.Services;

public class FoundryAgentResponsesClientFactoryTests
{
    [Fact]
    public void Factory_ResolvesEndpointThroughFoundryOpenAiEndpoint()
    {
        var factory = File.ReadAllText(
            RepoFiles.FindRepoFile("core-dotnet/core/AIWeather/Services/FoundryAgentResponsesClientFactory.cs"));

        // AZURE_FOUNDRY_PROD_PROJ_URL must resolve through FoundryOpenAiEndpoint.Resolve (which
        // appends /openai/v1 when missing). Passing the raw project URL resolves both calls to an
        // Azure-classic path that needs an explicit api-version query parameter neither call sends,
        // and CreateResponseStreamingAsync fails with "Missing required query parameter: api-version" -
        // see Factory_SendsRequestsAgainstOpenAiV1Endpoint below for a live-request regression test.
        Assert.Contains("FoundryOpenAiEndpoint.Resolve", factory, StringComparison.Ordinal);
        Assert.Contains("GetProjectResponsesClientForAgent", factory, StringComparison.Ordinal);
        Assert.Contains("CreateProjectConversationAsync", factory, StringComparison.Ordinal);
        Assert.DoesNotContain("ResolveProjectEndpoint", factory, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Factory_SendsRequestsAgainstOpenAiV1Endpoint()
    {
        using var listener = new HttpListener();
        var port = GetFreeTcpPort();
        var prefix = $"http://127.0.0.1:{port}/";
        listener.Prefixes.Add(prefix);
        listener.Start();

        Uri? capturedUri = null;
        var listenerTask = Task.Run(async () =>
        {
            var ctx = await listener.GetContextAsync();
            capturedUri = ctx.Request.Url;
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "application/json";
            var body = "{\"id\":\"conv_test\",\"object\":\"conversation\"}"u8.ToArray();
            ctx.Response.ContentLength64 = body.Length;
            await ctx.Response.OutputStream.WriteAsync(body);
            ctx.Response.OutputStream.Close();
        });

        // Endpoint-routing check only, so this authenticates with an API key rather than the
        // production TokenCredential path: BearerTokenPolicy (what the Uri/TokenCredential/Options
        // constructor uses - see Factory_TokenCredentialConstructionPathRefusesNonTlsEndpoint below)
        // refuses to send bearer tokens over a plain-HTTP endpoint, and this local listener isn't TLS.
        var (_, conversationId) = await FoundryAgentResponsesClientFactoryTestHelper.CreateForAgentWithApiKeyAsync(
            "test-agent",
            new Uri($"{prefix}api/projects/testproj"),
            "fake-key");

        await listenerTask;
        listener.Stop();

        Assert.Equal("conv_test", conversationId);
        Assert.NotNull(capturedUri);
        Assert.Contains("/openai/v1/", capturedUri!.AbsolutePath, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Factory_TokenCredentialConstructionPathRefusesNonTlsEndpoint()
    {
        // Proves the production Uri/TokenCredential/Options constructor overload actually ran
        // (not the ApiKeyCredential one): BearerTokenPolicy refuses a non-TLS endpoint before
        // ever touching the network, so no listener is needed here - the guard fires client-side.
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            FoundryAgentResponsesClientFactoryTestHelper.CreateForAgentAsync(
                "test-agent",
                new Uri("http://127.0.0.1:1/api/projects/testproj"),
                new FakeTokenCredential()));

        Assert.Contains("not permitted for non", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Factory_RequestsAiAzureComAudience()
    {
        // Agent publishing (prod-deploy-foundry-agents.yml) and the Wx1116GeoNonAIWeather
        // toolbox connection's ProjectManagedIdentity audience both use
        // https://ai.azure.com/.default for the Agents API - confirm this constructor overload
        // requests the same audience, not the cognitiveservices.azure.com one
        // FoundryResponsesClientFactory uses for direct model inference. A scope mismatch here
        // would 401 Chat3/V5 even with Foundry User correctly assigned.
        var capturedScopes = new List<string>();
        var credential = new CapturingTokenCredential(capturedScopes);

        // Nothing listens on 127.0.0.1:1 - the connection attempt fails, but only after
        // GetToken has already been called with the real requested scope.
        await Assert.ThrowsAnyAsync<Exception>(() =>
            FoundryAgentResponsesClientFactoryTestHelper.CreateForAgentAsync(
                "test-agent",
                new Uri("https://127.0.0.1:1/api/projects/testproj"),
                credential));

        Assert.Contains("https://ai.azure.com/.default", capturedScopes);
    }

    private static int GetFreeTcpPort()
    {
        var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}

/// <summary>
/// Duplicates <see cref="FoundryAgentResponsesClientFactory.CreateForAgentAsync(string, Uri, CancellationToken)"/>'s
/// construction with an injectable credential, so tests can point it at a local listener instead
/// of going through <see cref="FoundryTokenCredentialFactory"/>'s real managed identity /
/// DefaultAzureCredential (which would try to reach IMDS/AAD and stall or fail outside Azure).
/// </summary>
internal static class FoundryAgentResponsesClientFactoryTestHelper
{
    /// <summary>Api-key auth, for endpoint-routing tests that need a real successful round trip.</summary>
    public static async Task<(ProjectResponsesClient ResponseClient, string ConversationId)> CreateForAgentWithApiKeyAsync(
        string agentName,
        Uri endpoint,
        string apiKey)
    {
        var resolvedEndpoint = FoundryOpenAiEndpoint.Resolve(endpoint.ToString());

        var projectOpenAIClient = new ProjectOpenAIClient(
            ApiKeyAuthenticationPolicy.CreateHeaderApiKeyPolicy(new ApiKeyCredential(apiKey), "api-key"),
            new ProjectOpenAIClientOptions
            {
                Endpoint = resolvedEndpoint,
            });

        var responseClient = projectOpenAIClient.GetProjectResponsesClientForAgent(agentName);
        var conversation = (await projectOpenAIClient
            .GetProjectConversationsClient()
            .CreateProjectConversationAsync(new ConversationCreationOptions())).Value;

        return (responseClient, conversation.Id);
    }

    /// <summary>The same Uri/TokenCredential/Options overload the production factory always uses now.</summary>
    public static async Task<(ProjectResponsesClient ResponseClient, string ConversationId)> CreateForAgentAsync(
        string agentName,
        Uri endpoint,
        TokenCredential credential)
    {
        var resolvedEndpoint = FoundryOpenAiEndpoint.Resolve(endpoint.ToString());

        var projectOpenAIClient = new ProjectOpenAIClient(
            resolvedEndpoint,
            credential,
            new ProjectOpenAIClientOptions());

        var responseClient = projectOpenAIClient.GetProjectResponsesClientForAgent(agentName);
        var conversation = (await projectOpenAIClient
            .GetProjectConversationsClient()
            .CreateProjectConversationAsync(new ConversationCreationOptions())).Value;

        return (responseClient, conversation.Id);
    }
}

/// <summary>Returns a fixed fake token without any network call, for tests that need a TokenCredential offline.</summary>
internal sealed class FakeTokenCredential : TokenCredential
{
    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken) =>
        new("fake-token", DateTimeOffset.UtcNow.AddHours(1));

    public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken) =>
        new(GetToken(requestContext, cancellationToken));
}

/// <summary>Like <see cref="FakeTokenCredential"/>, but records the scopes the SDK actually requested.</summary>
internal sealed class CapturingTokenCredential(List<string> capturedScopes) : TokenCredential
{
    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        capturedScopes.AddRange(requestContext.Scopes);
        return new AccessToken("fake-token", DateTimeOffset.UtcNow.AddHours(1));
    }

    public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken) =>
        new(GetToken(requestContext, cancellationToken));
}
