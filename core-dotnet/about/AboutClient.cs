using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace Core.about;

/// <summary>
/// HTTP client that loads a remote <see cref="AboutNode"/> for inclusion in an About tree.
/// </summary>
public sealed class AboutClient(
    HttpClient httpClient,
    ILogger<AboutClient> logger) : IAboutClient
{
    public async Task<AboutNode> GetAsync(
        string? url,
        string expectedName,
        CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            logger.LogWarning("About URL for {ExpectedName} is missing or invalid", expectedName);
            return CreateUnhealthyNode(expectedName);
        }

        try
        {
            var node = await httpClient.GetFromJsonAsync<AboutNode>(uri, cancellationToken);
            if (node is null || !string.Equals(node.Name, expectedName, StringComparison.Ordinal))
            {
                logger.LogWarning(
                    "About endpoint {Url} did not return the expected {ExpectedName} node",
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
                "Could not load About node {ExpectedName} from {Url}",
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
