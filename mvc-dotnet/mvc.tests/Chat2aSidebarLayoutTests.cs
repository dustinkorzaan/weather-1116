using System.Net;
using System.Text.RegularExpressions;

namespace WeatherMVC.Tests;

// Implementation details not covered by Chat2aSidebarAcceptanceTests: script load order,
// toggle/Escape wiring, entry fields fed to chatRender, and the docked CSS layout.
public class Chat2aSidebarLayoutTests(WeatherMvcWebApplicationFactory factory) : IClassFixture<WeatherMvcWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static string ReadRepoFile(string relativePath) => File.ReadAllText(RepoFiles.FindRepoFile(relativePath));

    private async Task<string> GetHtmlAsync(string path)
    {
        var response = await _client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadAsStringAsync();
    }

    [Fact]
    public async Task Home_LoadsMarkdownAndRenderScriptsBeforeSidebarScript()
    {
        var html = await GetHtmlAsync("/");

        var positions = new[] { "js/lib/marked.min.js", "js/lib/purify.min.js", "js/markdown/safeGfmMarkdown.js", "js/chatRender.js", "js/chatSidebar.js" }
            .Select(script => html.IndexOf(script, StringComparison.Ordinal))
            .ToArray();

        Assert.All(positions, position => Assert.True(position >= 0));
        Assert.Equal(positions.Order(), positions);
    }

    [Theory]
    [InlineData("/hello-world")]
    [InlineData("/chat-clients")]
    public async Task OtherRoutes_DoNotLoadSidebarScript(string path)
    {
        var html = await GetHtmlAsync(path);

        Assert.DoesNotContain("js/chatSidebar.js", html);
    }

    [Fact]
    public void ChatSidebarScript_TogglesAriaExpandedAndClosesOnEscape()
    {
        var script = ReadRepoFile("mvc-dotnet/mvc/wwwroot/js/chatSidebar.js");

        Assert.Contains("sidebar.hidden = !open;", script);
        Assert.Contains("button.setAttribute('aria-expanded', open ? 'true' : 'false');", script);
        Assert.Contains("event.key === 'Escape'", script);
        Assert.Contains("closeButton.addEventListener('click'", script);
    }

    [Fact]
    public void ChatSidebarScript_KeepsToolDetailsAndUsageOnEntriesLikeChatClient()
    {
        var script = ReadRepoFile("mvc-dotnet/mvc/wwwroot/js/chatSidebar.js");

        Assert.Contains("toolName: payload.toolName,", script);
        Assert.Contains("toolArguments: payload.toolArguments,", script);
        Assert.Contains("running: true,", script);
        Assert.Contains("pending.running = false;", script);
        Assert.Contains("pending.toolResult = payload.toolResult;", script);
        Assert.Contains("assistantEntry.streaming = false;", script);
        Assert.Contains("assistantEntry.usage = payload.usage || null;", script);
        Assert.Contains("window.chatRender.attachToolHover(messagesEl);", script);
        Assert.DoesNotContain("innerHTML", script);
    }

    [Fact]
    public void SiteCss_DocksSidebarBesideMapAndStacksItBelow640px()
    {
        var css = ReadRepoFile("mvc-dotnet/mvc/wwwroot/css/site.css");

        var sidebar = Regex.Match(css, @"\n\.chat-sidebar \{[^}]*\}").Value;
        Assert.DoesNotContain("position: fixed", sidebar);
        Assert.Contains("flex: 0 0 24rem;", sidebar);
        Assert.Contains("var(--color-bg)", sidebar);

        var mapSection = Regex.Match(css, @"\n\.map-section \{[^}]*\}").Value;
        Assert.Contains("flex-direction: row;", mapSection);
        Assert.Matches(@"@media \(max-width: 639\.98px\) \{\s*\.map-section \{\s*flex-direction: column;", css);
        Assert.Matches(@"@media \(max-width: 639\.98px\) \{\s*\.chat-sidebar \{\s*width: 100%;\s*flex: 1 1 0;", css);

        var header = Regex.Match(css, @"\n\.site-header \{[^}]*\}").Value;
        Assert.DoesNotContain("z-index", header);
    }
}
