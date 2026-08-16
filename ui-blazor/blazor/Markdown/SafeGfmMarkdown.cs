using System.Net;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.Extensions.EmphasisExtras;

namespace WeatherBlazor.Markdown;

public static class SafeGfmMarkdown
{
    // Match React remark-gfm / MVC marked gfm: tables, strikethrough, autolinks, task lists.
    // Skip Markdig media embeds, math, diagrams, grid tables, and generic attributes.
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UsePipeTables()
        .UseEmphasisExtras(EmphasisExtraOptions.Strikethrough)
        .UseAutoLinks()
        .UseTaskLists()
        .DisableHtml()
        .Build();

    private static readonly Regex UrlAttribute = new(
        """\s(?<name>href|src)\s*=\s*(?:"(?<value>[^"]*)"|'(?<value>[^']*)'|(?<value>[^\s>]+))""",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static string ToHtml(string? markdown)
    {
        if (string.IsNullOrEmpty(markdown))
        {
            return string.Empty;
        }

        return UrlAttribute.Replace(Markdig.Markdown.ToHtml(markdown, Pipeline), match =>
            IsSafeUrl(match.Groups["value"].Value) ? match.Value : string.Empty);
    }

    private static bool IsSafeUrl(string url)
    {
        var decoded = WebUtility.HtmlDecode(url).Trim();
        if (decoded.Length == 0)
        {
            return true;
        }

        if (decoded[0] is '#' or '?' or '.')
        {
            return true;
        }

        if (decoded[0] == '/')
        {
            if (decoded.StartsWith("//", StringComparison.Ordinal))
            {
                return Uri.TryCreate("https:" + decoded, UriKind.Absolute, out var protocolRelative)
                    && IsAllowedScheme(protocolRelative.Scheme);
            }

            return true;
        }

        if (!Uri.TryCreate(decoded, UriKind.Absolute, out var uri))
        {
            return !decoded.Contains(':');
        }

        return IsAllowedScheme(uri.Scheme);
    }

    private static bool IsAllowedScheme(string scheme) =>
        scheme == Uri.UriSchemeHttp
        || scheme == Uri.UriSchemeHttps
        || scheme == Uri.UriSchemeMailto;
}
