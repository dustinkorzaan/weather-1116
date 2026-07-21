namespace Core.about;

public interface IMcpAboutClient
{
    Task<AboutNode> GetAsync(
        string? url,
        string expectedName,
        CancellationToken cancellationToken = default);
}
