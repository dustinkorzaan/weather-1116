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
    public void ToHtml_StripsRawHtmlAndUnsafeLinks()
    {
        var html = SafeGfmMarkdown.ToHtml("Hello <script>alert(1)</script> [x](javascript:alert(1))");

        Assert.DoesNotContain("<script", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("javascript:", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Hello", html);
    }
}
