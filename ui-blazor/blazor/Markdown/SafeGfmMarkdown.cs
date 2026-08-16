using System.Text.RegularExpressions;
using Markdig;

namespace WeatherBlazor.Markdown;

public static class SafeGfmMarkdown
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .DisableHtml()
        .Build();

    private static readonly Regex UnsafeUrlAttribute = new(
        @"\s(?:href|src)\s*=\s*""(?:javascript|data|vbscript):[^""]*""",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static string ToHtml(string? markdown)
    {
        if (string.IsNullOrEmpty(markdown))
        {
            return string.Empty;
        }

        return UnsafeUrlAttribute.Replace(Markdown.ToHtml(markdown, Pipeline), string.Empty);
    }
}
