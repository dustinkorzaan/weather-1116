using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace Core.About;

/// <summary>
/// HTTP client that loads a remote <see cref="AboutNode"/> for inclusion in an About tree.
/// </summary>
public sealed class AboutClient(
    HttpClient httpClient,
    ILogger<AboutClient> logger) : IAboutClient
{
    public async Task<AboutNode> GetAsync(
        string? url,
        string name,
        CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            logger.LogWarning("About URL for {name} is missing or invalid", name);
            return CreateUnhealthyNode(name);
        }

        try
        {
            var node = await httpClient.GetFromJsonAsync<AboutNode>(uri, cancellationToken);
            if (node is null || !string.Equals(node.Name, name, StringComparison.Ordinal))
            {
                logger.LogWarning(
                    "About endpoint {Url} did not return the expected {name} node",
                    uri,
                    name);
                return CreateUnhealthyNode(name);
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
                "Could not load About node {name} from {Url}",
                name,
                uri);
            return CreateUnhealthyNode(name);
        }
    }

    private static AboutNode CreateUnhealthyNode(string name)
        => new()
        {
            Name = name,
            IsHealthy = false,
        };
}
