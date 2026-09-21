using System.ClientModel;
using System.ClientModel.Primitives;
using System.Net;
using Azure.AI.Extensions.OpenAI;
using OpenAI.Conversations;

namespace Core.Tests.AIWeather.Services;

public class FoundryAgentResponsesClientFactoryTests
{
    [Fact]
    public void Factory_MatchesFoundryConsoleV5ClientConstruction()
    {
        var factory = File.ReadAllText(
            RepoFiles.FindRepoFile("core-dotnet/core/AIWeather/Services/FoundryAgentResponsesClientFactory.cs"));
        var console = File.ReadAllText(RepoFiles.FindRepoFile("FoundryConsoleV5/Program.cs"));

        // Console V5 is untouched and stays its own, independent implementation - the factory does not
        // call it, and this test only checks that the two stay in agreement on the parts that must
        // match (endpoint, agent, conversation calls), not that either references the other.
        Assert.Contains("Endpoint = endpoint", factory, StringComparison.Ordinal);
        Assert.Contains("Endpoint = new Uri(endpoint)", console, StringComparison.Ordinal);
        Assert.Contains("GetProjectResponsesClientForAgent", factory, StringComparison.Ordinal);
        Assert.Contains("GetProjectResponsesClientForAgent", console, StringComparison.Ordinal);
        Assert.Contains("CreateProjectConversationAsync", factory, StringComparison.Ordinal);
        Assert.Contains("CreateProjectConversationAsync", console, StringComparison.Ordinal);
        Assert.DoesNotContain("FoundryOpenAiEndpoint.Resolve", factory, StringComparison.Ordinal);
        Assert.DoesNotContain("ResolveProjectEndpoint", factory, StringComparison.Ordinal);

        // Console V5's single client (no AgentName) works for CreateProjectConversationAsync but fails
        // "Missing required query parameter: api-version" on the agent-scoped responses call - confirmed
        // against a live Foundry project. The factory therefore uses two ProjectOpenAIClient instances,
        // one per call, so each gets the api-version behavior its endpoint actually needs (see
        // Factory_ResponsesRequestCarriesApiVersion_ConversationsRequestDoesNot below). Console V5 itself
        // is left as-is; do not add AgentName there.
        Assert.Contains("AgentName = agentName", factory, StringComparison.Ordinal);
        Assert.DoesNotContain("AgentName =", console, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Factory_ResponsesRequestCarriesApiVersion_ConversationsRequestDoesNot()
    {
        using var listener = new HttpListener();
        var port = GetFreeTcpPort();
        var prefix = $"http://127.0.0.1:{port}/";
        listener.Prefixes.Add(prefix);
        listener.Start();

        var capturedUris = new List<Uri>();
        var listenerTask = Task.Run(async () =>
        {
            // Only the conversation-creation call happens during CreateForAgentAsync itself; capture it.
            var ctx = await listener.GetContextAsync();
            lock (capturedUris) { capturedUris.Add(ctx.Request.Url!); }
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "application/json";
            var body = "{\"id\":\"conv_test\",\"object\":\"conversation\"}"u8.ToArray();
            ctx.Response.ContentLength64 = body.Length;
            await ctx.Response.OutputStream.WriteAsync(body);
            ctx.Response.OutputStream.Close();
        });

        var (responseClient, conversationId) = await FoundryAgentResponsesClientFactoryTestHelper.CreateForAgentAsync(
            "test-agent",
            new Uri($"{prefix}api/projects/testproj"),
            "fake-key");

        await listenerTask;
        listener.Stop();

        Assert.Equal("conv_test", conversationId);
        Assert.NotNull(responseClient);

        var conversationsRequest = Assert.Single(capturedUris);
        Assert.Contains("/conversations", conversationsRequest.AbsolutePath, StringComparison.Ordinal);
        Assert.DoesNotContain("api-version=", conversationsRequest.Query, StringComparison.Ordinal);
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
/// Exercises the same construction as <see cref="FoundryAgentResponsesClientFactory.CreateForAgentAsync(string, Uri, CancellationToken)"/>
/// but with an injectable api key, so the test above can point it at a local listener instead of
/// reading <c>AZURE_FOUNDRY_PROD_KEY</c>.
/// </summary>
internal static class FoundryAgentResponsesClientFactoryTestHelper
{
    public static async Task<(ProjectResponsesClient ResponseClient, string ConversationId)> CreateForAgentAsync(
        string agentName,
        Uri endpoint,
        string apiKey)
    {
        var apiKeyPolicy = ApiKeyAuthenticationPolicy.CreateHeaderApiKeyPolicy(new ApiKeyCredential(apiKey), "api-key");

        var responsesHost = new ProjectOpenAIClient(
            apiKeyPolicy,
            new ProjectOpenAIClientOptions
            {
                Endpoint = endpoint,
                AgentName = agentName,
            });
        var responseClient = responsesHost.GetProjectResponsesClientForAgent(agentName);

        var conversationsHost = new ProjectOpenAIClient(
            apiKeyPolicy,
            new ProjectOpenAIClientOptions
            {
                Endpoint = endpoint,
            });
        var conversation = (await conversationsHost
            .GetProjectConversationsClient()
            .CreateProjectConversationAsync(new ConversationCreationOptions())).Value;

        return (responseClient, conversation.Id);
    }
}
