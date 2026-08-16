using Ganss.Xss;
using Markdig;

namespace WeatherBlazor.Data;

public static class ChatMarkdown
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .DisableHtml()
        .Build();

    private static readonly HtmlSanitizer Sanitizer = new();

    public static string ToHtml(string? markdown)
    {
        if (string.IsNullOrEmpty(markdown))
        {
            return string.Empty;
        }

        return Sanitizer.Sanitize(Markdown.ToHtml(markdown, Pipeline));
    }
}
