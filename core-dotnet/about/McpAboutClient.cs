using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace Core.about;

public sealed class McpAboutClient(
    HttpClient httpClient,
    ILogger<McpAboutClient> logger) : IMcpAboutClient
{
    public async Task<AboutNode> GetAsync(
        string? url,
        string expectedName,
        CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            logger.LogWarning("MCP About URL for {ExpectedName} is missing or invalid", expectedName);
            return CreateUnhealthyNode(expectedName);
        }

        try
        {
            var node = await httpClient.GetFromJsonAsync<AboutNode>(uri, cancellationToken);
            if (node is null || !string.Equals(node.Name, expectedName, StringComparison.Ordinal))
            {
                logger.LogWarning(
                    "MCP About endpoint {Url} did not return the expected {ExpectedName} node",
                    uri,
                    expectedName);
                return CreateUnhealthyNode(expectedName);
            }

            return node;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Could not load MCP About node {ExpectedName} from {Url}",
                expectedName,
                uri);
            return CreateUnhealthyNode(expectedName);
        }
    }

    private static AboutNode CreateUnhealthyNode(string name)
        => new()
        {
            Name = name,
            IsHealthy = false,
        };
}
