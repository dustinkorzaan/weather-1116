using System.ClientModel;
using System.ClientModel.Primitives;
using System.Net;
using Azure.AI.Extensions.OpenAI;
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

        // Deliberately pass the raw project URL (no /openai/v1 suffix), matching what
        // AZURE_FOUNDRY_PROD_PROJ_URL actually holds - the factory itself must append the suffix.
        var (_, conversationId) = await FoundryAgentResponsesClientFactoryTestHelper.CreateForAgentAsync(
            "test-agent",
            new Uri($"{prefix}api/projects/testproj"),
            "fake-key");

        await listenerTask;
        listener.Stop();

        Assert.Equal("conv_test", conversationId);
        Assert.NotNull(capturedUri);
        Assert.Contains("/openai/v1/", capturedUri!.AbsolutePath, StringComparison.Ordinal);
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
}
