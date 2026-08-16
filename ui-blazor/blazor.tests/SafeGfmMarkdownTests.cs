using WeatherBlazor.Markdown;

namespace WeatherBlazor.Tests;

public sealed class SafeGfmMarkdownTests
{
    [Fact]
    public void ToHtml_RendersGfmTablesAndEmphasis()
    {
        var html = SafeGfmMarkdown.ToHtml("""
            **Warmest** today:

            | City | Temp |
            | --- | --- |
            | Nashville | 72 |
            | Atlanta | 80 |
            """);

        Assert.Contains("<table>", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<strong>", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Nashville", html);
        Assert.Contains("Atlanta", html);
        Assert.DoesNotContain("| City |", html);
    }

    [Fact]
    public void ToHtml_RendersGfmStrikethrough()
    {
        var html = SafeGfmMarkdown.ToHtml("~~old~~ now 72F");

        Assert.Contains("<del>", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("old", html);
    }

    [Fact]
    public void ToHtml_StripsRawHtmlAndUnsafeLinks()
    {
        var html = SafeGfmMarkdown.ToHtml("Hello <script>alert(1)</script> [x](javascript:alert(1))");

        Assert.DoesNotContain("<script", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("javascript:", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Hello", html);
        Assert.Contains(">x</a>", html);
    }

    [Fact]
    public void ToHtml_StripsDataImageSources()
    {
        var html = SafeGfmMarkdown.ToHtml("![x](data:text/html;base64,PHNjcmlwdD5hbGVydCgxKTwvc2NyaXB0Pg==)");

        Assert.DoesNotContain("data:", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<script", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ToHtml_KeepsHttpsLinks()
    {
        var html = SafeGfmMarkdown.ToHtml("[NWS](https://www.weather.gov/)");

        Assert.Contains("href=\"https://www.weather.gov/\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("NWS", html);
    }

    [Fact]
    public void ToHtml_DoesNotEmbedMediaLinksAsIframes()
    {
        var html = SafeGfmMarkdown.ToHtml("[watch](https://www.youtube.com/watch?v=dQw4w9WgXcQ)");

        Assert.DoesNotContain("<iframe", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("href=\"https://www.youtube.com/watch?v=dQw4w9WgXcQ\"", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ToHtml_DoesNotRenderMathOrFootnoteExtensions()
    {
        var html = SafeGfmMarkdown.ToHtml("""
            $$x^2$$

            See the note.[^1]

            [^1]: leftover advanced extension
            """);

        Assert.DoesNotContain("math", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("footnote", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("$$x^2$$", html);
    }
}
