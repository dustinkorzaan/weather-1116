using System.Net;
using System.Text.RegularExpressions;

namespace WeatherMVC.Tests;

/// <summary>
/// Acceptance tests for docs/specs/2026-09-30-chat2a-sidebar.md (MVC).
/// MVC has no JS test runner, so AC3-AC5 are source assertions on the scripts
/// (the same pattern HomeControllerTests uses); the spec's manual check covers runtime.
/// </summary>
public class Chat2aSidebarAcceptanceTests(WeatherMvcWebApplicationFactory factory) : IClassFixture<WeatherMvcWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    // AC1
    [Fact]
    public async Task AC1_HomeHeaderActions_AreAddLocation_ThenOpenChat_ThenAvatar()
    {
        var html = await GetHtmlAsync("/");

        var addLocation = IndexOfRequired(html, "aria-label=\"Add location\"");
        var openChat = IndexOfRequired(html, "aria-label=\"Open chat\"");
        var avatar = IndexOfRequired(html, "aria-label=\"Open user menu\"");

        Assert.True(addLocation < openChat, "Add location must come before Open chat.");
        Assert.True(openChat < avatar, "Open chat must come before the avatar button.");

        var button = OpeningTag(html, "button", "aria-label=\"Open chat\"");
        Assert.Contains("aria-controls=\"chat2a-sidebar\"", button);
        Assert.Contains("aria-expanded=\"false\"", button);
        Assert.Contains("type=\"button\"", button);
    }

    // AC1 (edge: routes without the plus button)
    [Theory]
    [InlineData("/hello-world")]
    [InlineData("/current-ai-weather")]
    [InlineData("/chat-clients")]
    public async Task AC1_OnRoutesWithoutAddLocation_OpenChatSitsImmediatelyBeforeAvatar(string path)
    {
        var html = await GetHtmlAsync(path);

        Assert.DoesNotContain("aria-label=\"Add location\"", html);

        var openChatTag = Regex.Match(html, "<button[^>]*aria-label=\"Open chat\"[^>]*>");
        Assert.True(openChatTag.Success, $"Expected an Open chat button on {path}.");
        var avatar = IndexOfRequired(html, "aria-label=\"Open user menu\"");
        Assert.True(openChatTag.Index < avatar);

        // No other button sits between the chat button and the avatar button.
        var afterChatButton = html.IndexOf("</button>", openChatTag.Index, StringComparison.Ordinal);
        var avatarButtonStart = html.LastIndexOf("<button", avatar, StringComparison.Ordinal);
        Assert.True(afterChatButton > 0 && afterChatButton < avatarButtonStart);
        Assert.DoesNotContain("<button", html[(afterChatButton + "</button>".Length)..avatarButtonStart]);
    }

    // AC2
    [Theory]
    [InlineData("/")]
    [InlineData("/hello-world")]
    public async Task AC2_LayoutRendersHiddenChat2aComplementaryPanelWithCloseButton(string path)
    {
        var html = await GetHtmlAsync(path);

        var panelTag = Regex.Match(html, "<(\\w+)[^>]*\\bid=\"chat2a-sidebar\"[^>]*>");
        Assert.True(panelTag.Success, "Expected an element with id=\"chat2a-sidebar\".");
        Assert.Matches("role=\"(complementary|dialog)\"", panelTag.Value);
        Assert.Contains("aria-label=\"Chat2a\"", panelTag.Value);
        Assert.Matches("\\shidden(\\s|>|=)", panelTag.Value);

        // The panel lives outside the top bar (it sits below it).
        var headerEnd = IndexOfRequired(html, "</header>");
        Assert.True(panelTag.Index > headerEnd, "The sidebar must not be inside the header.");

        var panelEnd = html.IndexOf($"</{panelTag.Groups[1].Value}>", panelTag.Index, StringComparison.Ordinal);
        var panel = html[panelTag.Index..panelEnd];
        Assert.Matches("<button[^>]*aria-label=\"Close chat\"", panel);
        Assert.Contains("id=\"chat2a-sidebar-input\"", panel);

        Assert.Contains("js/chatSidebar.js", html);
    }

    // AC2 (edge: sidebar ids must not collide with the /chat-clients page chat)
    [Fact]
    public async Task AC2_OnChatClients_SidebarDoesNotDuplicatePageChatIds()
    {
        var html = await GetHtmlAsync("/chat-clients");

        Assert.Single(Regex.Matches(html, "id=\"chat2a-sidebar\""));
        Assert.Single(Regex.Matches(html, "id=\"chat-input\""));
        Assert.Single(Regex.Matches(html, "id=\"chat-messages\""));
    }

    // AC2 (toggle, Close chat, Escape)
    [Fact]
    public void AC2_ChatSidebarScript_TogglesHiddenAndAriaExpanded_AndClosesOnEscape()
    {
        var script = ReadRepoFile("mvc-dotnet/mvc/wwwroot/js/chatSidebar.js");

        Assert.Contains("chat2a-sidebar", script);
        Assert.Contains("chatSidebarButton", script);
        Assert.Contains("aria-expanded", script);
        Assert.Contains("hidden", script);
        Assert.Matches("['\"]Escape['\"]", script);
    }

    // AC2 (right edge, full width below 640px)
    [Fact]
    public void AC2_SiteCss_AnchorsSidebarRight_AndFullWidthUnder640()
    {
        var css = ReadRepoFile("mvc-dotnet/mvc/wwwroot/css/site.css");

        var declarations = string.Join(
            "\n",
            Regex.Matches(css, @"([^{}]*(?:chat2a-sidebar|chat-sidebar)[^{}]*)\{([^{}]*)\}")
                .Select(match => match.Groups[2].Value));
        Assert.Matches(@"position:\s*fixed", declarations);
        Assert.Matches(@"right:\s*0", declarations);
        Assert.Matches(@"top:\s*\S", declarations);

        var narrowBlocks = Regex.Matches(css, @"@media[^{]*max-width:\s*6[0-3]\d(?:\.\d+)?px[^{]*\{((?:[^{}]*\{[^{}]*\})*)[^{}]*\}")
            .Select(match => match.Groups[1].Value)
            .Where(body => Regex.IsMatch(body, @"(?:chat2a-sidebar|chat-sidebar)[^{}]*\{[^{}]*width:\s*100%"));
        Assert.NotEmpty(narrowBlocks);
    }

    // AC3
    [Fact]
    public void AC3_ChatSidebarScript_PostsSessionIdAndMessageToChat2a_AndRendersStreamEvents()
    {
        var script = ReadRepoFile("mvc-dotnet/mvc/wwwroot/js/chatSidebar.js");

        Assert.Matches("['\"`]/Chat2a/messages['\"`]", script);
        Assert.Matches(@"method:\s*['""]POST['""]", script);
        Assert.Matches(@"JSON\.stringify\(\{\s*sessionId(\s*:\s*\w+)?\s*,\s*message(\s*:\s*\w+)?\s*\}\)", script);

        // Only Chat2a: no other chat endpoint and no guardrail gate fields in the body.
        Assert.DoesNotMatch(@"/Chat(1a|1b|2b|3|4a|4b|5a|5b)/messages", script);
        Assert.DoesNotContain("enableMaxLengthGate", script);

        // Session id from the stream's `session` event is reused on later sends.
        Assert.Matches("['\"]session['\"]", script);
        Assert.Contains(".sessionId", script);

        foreach (var eventType in new[] { "token", "tool_start", "tool_end", "error" })
        {
            Assert.Matches($"['\"]{eventType}['\"]", script);
        }

        // Sidebar must not bind to the /chat-clients page chat elements.
        Assert.DoesNotMatch("['\"#]chat-input['\"]", script);
        Assert.DoesNotMatch("['\"#]chat-messages['\"]", script);
    }

    // AC4
    [Fact]
    public void AC4_ChatSidebarScript_RefreshesCitiesInFinally_WithUserFallback()
    {
        var script = ReadRepoFile("mvc-dotnet/mvc/wwwroot/js/chatSidebar.js");

        // Some `finally` block (runs on success and on failure) refreshes the pins.
        var refreshBlock = Regex.Matches(script, @"\bfinally\s*\{")
            .Select(match => BlockAfter(script[match.Index..], "finally"))
            .FirstOrDefault(block => block.Contains("refreshCities", StringComparison.Ordinal));
        Assert.True(refreshBlock is not null, "Expected a refreshCities call in a finally block.");

        // The finally block may inline the refresh or call a local helper of the same name.
        var refreshLogic = refreshBlock;
        var helper = Regex.Match(script, @"function\s+(\w*[Rr]efresh\w*)\s*\(");
        if (!refreshBlock.Contains("weatherMap", StringComparison.Ordinal) && helper.Success
            && refreshBlock.Contains(helper.Groups[1].Value + "(", StringComparison.Ordinal))
        {
            refreshLogic = BlockAfter(script[helper.Index..], "function");
        }

        Assert.Contains("weatherMap", refreshLogic);
        Assert.Contains("refreshCities", refreshLogic);
        // Routes without weatherMap.js still issue a GET /User per completed send.
        Assert.Matches(@"fetch\(\s*['""]/User['""]", refreshLogic);
        Assert.DoesNotContain("location.reload", script);
    }

    // AC5
    [Fact]
    public void AC5_WeatherMapScript_ExportsRefreshCities_WhichRereadsUserAndRepaintsPins()
    {
        var script = ReadRepoFile("mvc-dotnet/mvc/wwwroot/js/weatherMap.js");

        var exports = Regex.Match(script, @"return\s*\{[^{}]*\}\s*;\s*\}\)\(\);\s*$", RegexOptions.Singleline);
        Assert.True(exports.Success, "Expected the weatherMap IIFE to end with a returned export object.");
        Assert.Matches(@"refreshCities\s*:\s*refreshCities", exports.Value);

        var refresh = BlockAfter(script, "function refreshCities(");
        Assert.Contains("'/User'", refresh);
        Assert.Contains("renderCities(", refresh);

        var render = BlockAfter(script, "function renderCities(");
        Assert.Contains("setMap(null)", render);
        Assert.Contains("createCityMarkers(", render);
    }

    // ---- helpers ----

    private async Task<string> GetHtmlAsync(string path)
    {
        var response = await _client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadAsStringAsync();
    }

    private static string ReadRepoFile(string relativePath)
        => File.ReadAllText(RepoFiles.FindRepoFile(relativePath));

    private static int IndexOfRequired(string text, string value)
    {
        var index = text.IndexOf(value, StringComparison.Ordinal);
        Assert.True(index >= 0, $"Expected to find {value}.");
        return index;
    }

    private static string OpeningTag(string html, string tag, string containing)
    {
        var match = Regex.Match(html, $"<{tag}[^>]*{Regex.Escape(containing)}[^>]*>");
        Assert.True(match.Success, $"Expected a <{tag}> with {containing}.");
        return match.Value;
    }

    /// <summary>Returns the brace-balanced block that follows the first occurrence of <paramref name="marker"/>.</summary>
    private static string BlockAfter(string script, string marker)
    {
        var start = IndexOfRequired(script, marker);
        var open = script.IndexOf('{', start);
        Assert.True(open >= 0, $"Expected a block after {marker}.");
        var depth = 0;
        for (var i = open; i < script.Length; i++)
        {
            if (script[i] == '{')
            {
                depth++;
            }
            else if (script[i] == '}' && --depth == 0)
            {
                return script[open..(i + 1)];
            }
        }

        return script[open..];
    }
}
