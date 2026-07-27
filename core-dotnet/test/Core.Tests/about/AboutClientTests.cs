using System.Net;
using System.Text;
using System.Text.Json;
using Core.about;
using Microsoft.Extensions.Logging.Abstractions;

namespace Core.Tests.about;

public class AboutClientTests
{
    private const string NodeName = "mcp-dotnet";

    [Fact]
    public async Task GetAsync_ValidNode_ReturnsRemoteNode()
    {
        var payload = new AboutNode { Name = NodeName, IsHealthy = true };
        var client = CreateClient(JsonResponse(payload), out var recorded);

        var node = await client.GetAsync("https://example.com/about", NodeName);

        Assert.Equal(NodeName, node.Name);
        Assert.True(node.IsHealthy);
        Assert.Equal("https://example.com/about", recorded.LastRequestUri?.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com/about")]
    public async Task GetAsync_MissingOrNonHttpUrl_ReturnsUnhealthyNodeWithoutCallingNetwork(string? url)
    {
        var client = CreateClient(JsonResponse(new AboutNode { Name = NodeName }), out var recorded);

        var node = await client.GetAsync(url, NodeName);

        Assert.Equal(NodeName, node.Name);
        Assert.False(node.IsHealthy);
        Assert.Null(recorded.LastRequestUri);
    }

    [Fact]
    public async Task GetAsync_NameMismatch_ReturnsUnhealthyNode()
    {
        var payload = new AboutNode { Name = "something-else", IsHealthy = true };
        var client = CreateClient(JsonResponse(payload), out _);

        var node = await client.GetAsync("https://example.com/about", NodeName);

        Assert.Equal(NodeName, node.Name);
        Assert.False(node.IsHealthy);
    }

    [Fact]
    public async Task GetAsync_NullJsonBody_ReturnsUnhealthyNode()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null", Encoding.UTF8, "application/json"),
        };
        var client = CreateClient(response, out _);

        var node = await client.GetAsync("https://example.com/about", NodeName);

        Assert.Equal(NodeName, node.Name);
        Assert.False(node.IsHealthy);
    }

    [Fact]
    public async Task GetAsync_HttpError_ReturnsUnhealthyNode()
    {
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        var client = CreateClient(response, out _);

        var node = await client.GetAsync("https://example.com/about", NodeName);

        Assert.Equal(NodeName, node.Name);
        Assert.False(node.IsHealthy);
    }

    [Fact]
    public async Task GetAsync_TransportException_ReturnsUnhealthyNode()
    {
        var client = new AboutClient(
            new HttpClient(new ThrowingHandler(new HttpRequestException("boom"))),
            NullLogger<AboutClient>.Instance);

        var node = await client.GetAsync("https://example.com/about", NodeName);

        Assert.Equal(NodeName, node.Name);
        Assert.False(node.IsHealthy);
    }

    [Fact]
    public async Task GetAsync_CancellationRequested_Throws()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var client = new AboutClient(
            new HttpClient(new ThrowingHandler(new OperationCanceledException())),
            NullLogger<AboutClient>.Instance);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.GetAsync("https://example.com/about", NodeName, cts.Token));
    }

    private static HttpResponseMessage JsonResponse(AboutNode node)
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(node),
                Encoding.UTF8,
                "application/json"),
        };

    private static AboutClient CreateClient(HttpResponseMessage response, out RecordingHandler handler)
    {
        handler = new RecordingHandler(response);
        return new AboutClient(new HttpClient(handler), NullLogger<AboutClient>.Instance);
    }

    private sealed class RecordingHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        public Uri? LastRequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            return Task.FromResult(response);
        }
    }

    private sealed class ThrowingHandler(Exception exception) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw exception;
        }
    }
}
