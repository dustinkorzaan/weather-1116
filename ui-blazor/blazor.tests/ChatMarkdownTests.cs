using WeatherBlazor.Data;

namespace WeatherBlazor.Tests;

public sealed class ChatMarkdownTests
{
    [Fact]
    public void ToHtml_RendersGfmTablesAndEmphasis()
    {
        var html = ChatMarkdown.ToHtml("""
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
        var html = ChatMarkdown.ToHtml("Hello <script>alert(1)</script> [x](javascript:alert(1))");

        Assert.DoesNotContain("<script", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("javascript:", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Hello", html);
    }
}
