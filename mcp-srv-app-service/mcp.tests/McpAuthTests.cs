using System.Net;
using System.Text;

namespace WeatherMcpSrvAppService.Tests;

public class McpAuthTests : IClassFixture<WeatherMcpSrvAppServiceWebApplicationFactory>
{
    private const string TestKey = "integration-test-mcp-key";

    private readonly WeatherMcpSrvAppServiceWebApplicationFactory _factory;

    public McpAuthTests(WeatherMcpSrvAppServiceWebApplicationFactory factory)
    {
        _factory = factory.WithSetting("MCP_SRV_APP_SERVICE_KEY", TestKey);
    }

    [Fact]
    public async Task McpEndpoint_ReturnsUnauthorized_WithoutSecret()
    {
        using var client = _factory.CreateClient();
        using var request = CreateInitializeRequest();

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task McpEndpoint_ReturnsUnauthorized_WithWrongAuthenticationSecret()
    {
        using var client = _factory.CreateClient();
        using var request = CreateInitializeRequest("Authentication", "Bearer wrong-key");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("Authorization", "Bearer integration-test-mcp-key")]
    [InlineData("Authorization", "integration-test-mcp-key")]
    [InlineData("Authentication", "Bearer integration-test-mcp-key")]
    [InlineData("Authentication", "integration-test-mcp-key")]
    public async Task McpEndpoint_AcceptsSharedSecret_OnAuthorizationOrAuthentication(
        string headerName,
        string headerValue)
    {
        using var client = _factory.CreateClient();
        using var request = CreateInitializeRequest(headerName, headerValue);

        var response = await client.SendAsync(request);

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.True(
            (int)response.StatusCode is >= 200 and < 300,
            $"Expected MCP initialize to succeed, got {(int)response.StatusCode} {response.StatusCode}.");
    }

    private static HttpRequestMessage CreateInitializeRequest(string? headerName = null, string? headerValue = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent(
                """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-03-26","capabilities":{},"clientInfo":{"name":"mcp-auth-tests","version":"0"}}}""",
                Encoding.UTF8,
                "application/json"),
        };
        request.Headers.TryAddWithoutValidation("Accept", "application/json, text/event-stream");
        if (headerName is not null && headerValue is not null)
        {
            request.Headers.TryAddWithoutValidation(headerName, headerValue);
        }

        return request;
    }
}
