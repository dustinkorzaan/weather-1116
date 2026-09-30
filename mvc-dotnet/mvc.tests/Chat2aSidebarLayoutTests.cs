using System.Net;
using System.Text.RegularExpressions;

namespace WeatherMVC.Tests;

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
    public async Task Home_HeaderOrdersAddLocationThenOpenChatThenAvatar()
    {
        var html = await GetHtmlAsync("/");

        var addLocation = html.IndexOf("aria-label=\"Add location\"", StringComparison.Ordinal);
        var openChat = html.IndexOf("aria-label=\"Open chat\"", StringComparison.Ordinal);
        var avatar = html.IndexOf("aria-label=\"Open user menu\"", StringComparison.Ordinal);

        Assert.True(addLocation >= 0, "Add location button missing");
        Assert.True(addLocation < openChat, "Open chat must follow Add location");
        Assert.True(openChat < avatar, "Open chat must precede the avatar button");
    }

    [Theory]
    [InlineData("/hello-world")]
    [InlineData("/chat-clients")]
    public async Task OtherRoutes_RenderOpenChatImmediatelyBeforeAvatar(string path)
    {
        var html = await GetHtmlAsync(path);

        Assert.DoesNotContain("aria-label=\"Add location\"", html);
        var openChat = html.IndexOf("aria-label=\"Open chat\"", StringComparison.Ordinal);
        var avatar = html.IndexOf("aria-label=\"Open user menu\"", StringComparison.Ordinal);
        Assert.True(openChat >= 0 && openChat < avatar);
    }

    [Fact]
    public async Task Layout_RendersClosedToggleAndHiddenSidebar()
    {
        var html = await GetHtmlAsync("/");

        var toggle = Regex.Match(html, "<button id=\"chatSidebarButton\"[^>]*>", RegexOptions.Singleline).Value;
        Assert.Contains("aria-expanded=\"false\"", toggle);
        Assert.Contains("aria-controls=\"chat2a-sidebar\"", toggle);
        Assert.Contains("type=\"button\"", toggle);

        var aside = Regex.Match(html, "<aside id=\"chat2a-sidebar\"[^>]*>", RegexOptions.Singleline).Value;
        Assert.Contains("role=\"complementary\"", aside);
        Assert.Contains("aria-label=\"Chat2a\"", aside);
        Assert.Contains("hidden", aside);

        Assert.Contains("aria-label=\"Close chat\"", html);
        Assert.Contains("id=\"chat2a-sidebar-input\"", html);
        Assert.Contains("/js/chatSidebar.js", html);
    }

    [Fact]
    public async Task ChatClients_SidebarIdsDoNotCollideWithPageChat()
    {
        var html = await GetHtmlAsync("/chat-clients");

        foreach (var id in new[] { "chat-input", "chat-messages", "chat-form", "chat-send", "chat2a-sidebar-input", "chat2a-sidebar-messages" })
        {
            Assert.Single(Regex.Matches(html, $"id=\"{id}\""));
        }

        var sidebar = ReadRepoFile("mvc-dotnet/mvc/Views/Shared/_Chat2aSidebar.cshtml");
        Assert.DoesNotContain("class=\"chat-tab", sidebar);
        Assert.DoesNotContain("class=\"chat-messages\"", sidebar);
    }

    [Fact]
    public void ChatSidebarScript_PostsChat2aWithSessionAndRendersEvents()
    {
        var script = ReadRepoFile("mvc-dotnet/mvc/wwwroot/js/chatSidebar.js");

        Assert.Contains("fetch('/Chat2a/messages'", script);
        Assert.Contains("JSON.stringify({ sessionId, message })", script);
        Assert.Contains("let sessionId = null;", script);
        Assert.Contains("payload.type === 'session'", script);
        Assert.Contains("sessionId = payload.sessionId;", script);
        Assert.Contains("payload.type === 'token'", script);
        Assert.Contains("payload.type === 'tool_start'", script);
        Assert.Contains("payload.type === 'tool_end'", script);
        Assert.Contains("payload.type === 'error'", script);
        Assert.DoesNotContain("innerHTML", script);
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
    public void ChatSidebarScript_RefreshesCitiesInFinallyWithUserFallback()
    {
        var script = ReadRepoFile("mvc-dotnet/mvc/wwwroot/js/chatSidebar.js");

        var finallyBlock = script[script.IndexOf("} finally {", StringComparison.Ordinal)..];
        Assert.Contains("refreshCities();", finallyBlock);
        Assert.Contains("window.weatherMap.refreshCities()", script);
        Assert.Contains("fetch('/User', { headers: { Accept: 'application/json' } })", script);
    }

    [Fact]
    public void WeatherMapScript_ExportsRefreshCities()
    {
        var script = ReadRepoFile("mvc-dotnet/mvc/wwwroot/js/weatherMap.js");

        Assert.Contains("refreshCities: refreshCities,", script);
    }

    [Fact]
    public void SiteCss_AnchorsSidebarRightAndFullWidthOnNarrowScreens()
    {
        var css = ReadRepoFile("mvc-dotnet/mvc/wwwroot/css/site.css");

        var sidebar = Regex.Match(css, @"\n\.chat-sidebar \{[^}]*\}").Value;
        Assert.Contains("position: fixed;", sidebar);
        Assert.Contains("right: 0;", sidebar);
        Assert.Contains("bottom: 0;", sidebar);
        Assert.Contains("width: 24rem;", sidebar);
        Assert.Contains("var(--color-bg)", sidebar);
        Assert.Matches(@"@media \(max-width: 639\.98px\) \{\s*\.chat-sidebar \{\s*width: 100%;", css);
    }
}
