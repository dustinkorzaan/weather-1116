using System.ClientModel;
using System.ClientModel.Primitives;
using System.Net;
using System.Net.Http;
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

        // The TokenCredential ProjectOpenAIClient ctor takes the project URL and appends
        // /openai/v1 itself. Passing FoundryOpenAiEndpoint.Resolve(...) (already .../openai/v1)
        // double-appends and CreateProjectConversationAsync 404s.
        Assert.Contains("FoundryOpenAiEndpoint.ResolveProjectEndpoint", factory, StringComparison.Ordinal);
        Assert.Contains("GetProjectResponsesClientForAgent", factory, StringComparison.Ordinal);
        Assert.Contains("CreateProjectConversationAsync", factory, StringComparison.Ordinal);
        Assert.DoesNotContain("FoundryOpenAiEndpoint.Resolve(", factory, StringComparison.Ordinal);
    }

    [Fact]
    public void Factory_AuthenticatesViaFoundryTokenCredentialFactory()
    {
        // The tests below inject a fake TokenCredential into a duplicate helper (necessary to
        // point the SDK at a local listener instead of real IMDS/AAD) - that duplication means
        // nothing else pins the real factory to actually call FoundryTokenCredentialFactory.Create()
        // rather than, say, some other credential source. Pin it here.
        var factory = File.ReadAllText(
            RepoFiles.FindRepoFile("core-dotnet/core/AIWeather/Services/FoundryAgentResponsesClientFactory.cs"));

        Assert.Contains("FoundryTokenCredentialFactory.Create()", factory, StringComparison.Ordinal);
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
        // Agent publishing (prod-deploy-foundry-agents.yml), FoundryResponsesClientFactory
        // inference, and the Wx1116GeoNonAIWeather toolbox connection all use
        // https://ai.azure.com/.default. Confirm this constructor overload requests that
        // audience. A cognitiveservices.azure.com token 401s against the project endpoint.
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

    [Theory]
    [InlineData("https://example.services.ai.azure.com/api/projects/testproj")]
    [InlineData("https://example.services.ai.azure.com/api/projects/testproj/openai/v1")]
    public async Task Factory_TokenCredentialConversationPathDoesNotDoubleOpenAiSuffix(string endpoint)
    {
        var handler = new CapturingHttpHandler();
        var options = new ProjectOpenAIClientOptions
        {
            Transport = new HttpClientPipelineTransport(new HttpClient(handler)),
        };

        var (_, conversationId) = await FoundryAgentResponsesClientFactoryTestHelper.CreateForAgentAsync(
            "test-agent",
            new Uri(endpoint),
            new FakeTokenCredential(),
            options);

        Assert.Equal("conv_test", conversationId);
        Assert.NotNull(handler.Uri);
        Assert.Equal("/api/projects/testproj/openai/v1/conversations", handler.Uri!.AbsolutePath);
        Assert.DoesNotContain("/openai/v1/openai/v1", handler.Uri.AbsolutePath, StringComparison.Ordinal);
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
        TokenCredential credential,
        ProjectOpenAIClientOptions? options = null)
    {
        var projectEndpoint = FoundryOpenAiEndpoint.ResolveProjectEndpoint(endpoint.ToString());

        var projectOpenAIClient = new ProjectOpenAIClient(
            projectEndpoint,
            credential,
            options ?? new ProjectOpenAIClientOptions());

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

/// <summary>
/// Records the first request URI so tests can assert the TokenCredential
/// <c>ProjectOpenAIClient</c> path does not double-append <c>/openai/v1</c>.
/// </summary>
internal sealed class CapturingHttpHandler : HttpMessageHandler
{
    public Uri? Uri { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Uri = request.RequestUri;
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"id\":\"conv_test\",\"object\":\"conversation\"}", System.Text.Encoding.UTF8, "application/json"),
        };
        return Task.FromResult(response);
    }
}
