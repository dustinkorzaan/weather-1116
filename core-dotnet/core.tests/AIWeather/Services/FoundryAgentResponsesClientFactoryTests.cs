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

        Assert.Contains("Endpoint = endpoint", factory, StringComparison.Ordinal);
        Assert.Contains("Endpoint = new Uri(endpoint)", console, StringComparison.Ordinal);
        Assert.Contains("GetProjectResponsesClientForAgent", factory, StringComparison.Ordinal);
        Assert.Contains("GetProjectResponsesClientForAgent", console, StringComparison.Ordinal);
        Assert.Contains("CreateProjectConversationAsync", factory, StringComparison.Ordinal);
        Assert.Contains("CreateProjectConversationAsync", console, StringComparison.Ordinal);
        // Both must set AgentName: without it, ProjectOpenAIClient.CreatePipeline (Azure.AI.Extensions.OpenAI
        // 3.0.0-beta.2) never attaches "api-version" to outgoing requests, and Foundry rejects every call
        // with "Missing required query parameter: api-version" - see
        // Factory_SendsApiVersionQueryParameterOnEveryRequest below for a live-request regression test.
        Assert.Contains("AgentName = agentName", factory, StringComparison.Ordinal);
        Assert.Contains("AgentName = agentName", console, StringComparison.Ordinal);
        Assert.DoesNotContain("FoundryOpenAiEndpoint.Resolve", factory, StringComparison.Ordinal);
        Assert.DoesNotContain("ResolveProjectEndpoint", factory, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Factory_SendsApiVersionQueryParameterOnEveryRequest()
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

        var (_, conversationId) = await FoundryAgentResponsesClientFactoryTestHelper.CreateForAgentAsync(
            "test-agent",
            new Uri($"{prefix}api/projects/testproj"),
            "fake-key");

        await listenerTask;
        listener.Stop();

        Assert.Equal("conv_test", conversationId);
        Assert.NotNull(capturedUri);
        Assert.Contains("api-version=", capturedUri!.Query, StringComparison.Ordinal);
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
        var projectOpenAIClient = new ProjectOpenAIClient(
            ApiKeyAuthenticationPolicy.CreateHeaderApiKeyPolicy(new ApiKeyCredential(apiKey), "api-key"),
            new ProjectOpenAIClientOptions
            {
                Endpoint = endpoint,
                AgentName = agentName,
            });

        var responseClient = projectOpenAIClient.GetProjectResponsesClientForAgent(agentName);
        var conversation = (await projectOpenAIClient
            .GetProjectConversationsClient()
            .CreateProjectConversationAsync(new ConversationCreationOptions())).Value;

        return (responseClient, conversation.Id);
    }
}
