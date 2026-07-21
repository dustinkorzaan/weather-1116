namespace Core.about;

/// <summary>
/// Fetches a remote About node from any absolute HTTP(S) URL.
/// Currently used for MCP hosts; can later cover other dependency endpoints.
/// </summary>
public interface IAboutClient
{
    Task<AboutNode> GetAsync(
        string? url,
        string expectedName,
        CancellationToken cancellationToken = default);
}
