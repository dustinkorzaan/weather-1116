namespace Core.about;

/// <summary>
/// Builds absolute About probe URLs from configured service base URLs.
/// </summary>
public static class AboutEndpointUrls
{
    public const string AboutPath = "/about";

    public static string? ToAboutUrl(string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return null;
        }

        return $"{baseUrl.TrimEnd('/')}{AboutPath}";
    }
}
