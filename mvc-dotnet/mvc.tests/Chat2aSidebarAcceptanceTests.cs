using System.Net;
using System.Text.RegularExpressions;

namespace WeatherMVC.Tests;

/// <summary>
/// Acceptance tests for docs/specs/2026-09-30-chat2a-sidebar.md (MVC).
/// Served HTML is asserted through <see cref="WeatherMvcWebApplicationFactory"/>. MVC has no JS
/// test runner, so script behaviour (AC2 toggling, AC3-AC6) is asserted on the script sources
/// (the same pattern HomeControllerTests uses); the spec's manual check covers runtime.
/// </summary>
public class Chat2aSidebarAcceptanceTests(WeatherMvcWebApplicationFactory factory) : IClassFixture<WeatherMvcWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    public static TheoryData<string> OtherRoutes => new() { "/hello-world", "/current-ai-weather", "/chat-clients" };

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
        Assert.True(openChat < IndexOfRequired(html, "</header>"), "Open chat must be in the top bar.");

        var button = OpeningTag(html, "button", "aria-label=\"Open chat\"");
        Assert.Contains("aria-controls=\"chat2a-sidebar\"", button);
        Assert.Contains("aria-expanded=\"false\"", button);
        Assert.Contains("type=\"button\"", button);
    }

    // AC1 (negative: the chat button exists only on the map page)
    [Theory]
    [MemberData(nameof(OtherRoutes))]
    public async Task AC1_OnEveryOtherRoute_ThereIsNoOpenChatButton(string path)
    {
        var html = await GetHtmlAsync(path);

        Assert.DoesNotContain("aria-label=\"Open chat\"", html);
        Assert.DoesNotContain("aria-label=\"Add location\"", html);
        Assert.Contains("aria-label=\"Open user menu\"", html);
    }

    // AC2
    [Fact]
    public async Task AC2_Home_RendersHiddenChat2aComplementaryPanel_WithCloseButton()
    {
        var html = await GetHtmlAsync("/");

        var panelTag = PanelTag(html);
        Assert.Equal("aside", panelTag.Groups[1].Value);
        Assert.Contains("role=\"complementary\"", panelTag.Value);
        Assert.Contains("aria-label=\"Chat2a\"", panelTag.Value);
        Assert.Matches("\\shidden(\\s|>|=)", panelTag.Value);

        // The panel lives outside the top bar (it sits below it).
        Assert.True(panelTag.Index > IndexOfRequired(html, "</header>"), "The sidebar must not be inside the header.");

        var panelEnd = html.IndexOf($"</{panelTag.Groups[1].Value}>", panelTag.Index, StringComparison.Ordinal);
        var panel = html[panelTag.Index..panelEnd];
        Assert.Matches("<button[^>]*aria-label=\"Close chat\"", panel);
        Assert.Contains("id=\"chat2a-sidebar-input\"", panel);

        Assert.Single(Regex.Matches(html, "id=\"chat2a-sidebar\""));
        Assert.Single(Regex.Matches(html, "js/chatSidebar\\.js"));
    }

    // AC2 (negative: no sidebar, and no sidebar script, off the map page)
    [Theory]
    [MemberData(nameof(OtherRoutes))]
    public async Task AC2_OnEveryOtherRoute_ThereIsNoSidebarOrSidebarScript(string path)
    {
        var html = await GetHtmlAsync(path);

        Assert.DoesNotContain("id=\"chat2a-sidebar\"", html);
        Assert.DoesNotContain("chat2a-sidebar-input", html);
        Assert.DoesNotContain("js/chatSidebar.js", html);
    }

    // AC2 (docked beside the map in the page layout)
    [Fact]
    public async Task AC2_Home_PanelIsDockedAfterTheMap_InTheSameRowInsideMain()
    {
        var html = await GetHtmlAsync("/");

        var mapTag = Regex.Match(html, "<section[^>]*aria-label=\"Map\"[^>]*>");
        Assert.True(mapTag.Success, "Expected the map section.");
        var panelTag = PanelTag(html);
        Assert.True(panelTag.Index > mapTag.Index, "The sidebar must come after the map.");

        var mainStart = IndexOfRequired(html, "<main");
        var mainEnd = IndexOfRequired(html, "</main>");
        Assert.True(panelTag.Index > mainStart && panelTag.Index < mainEnd, "The sidebar must be in the page content, not an overlay outside <main>.");

        var row = CommonAncestorTag(html, mapTag.Index, panelTag.Index);
        Assert.NotNull(row);
        Assert.Matches("class=\"[^\"]+\"", row);
    }

    // AC2 (not a fixed overlay; row at >= 640px, stacked below)
    [Fact]
    public async Task AC2_SiteCss_DocksSidebarInFlow_RowAt640_StackedBelow()
    {
        var html = await GetHtmlAsync("/");
        var row = CommonAncestorTag(html, Regex.Match(html, "<section[^>]*aria-label=\"Map\"").Index, PanelTag(html).Index);
        Assert.NotNull(row);
        var rowClasses = Regex.Match(row, "class=\"([^\"]*)\"").Groups[1].Value
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);
        Assert.NotEmpty(rowClasses);

        var css = ReadRepoFile("mvc-dotnet/mvc/wwwroot/css/site.css");

        // No rule for the sidebar element itself makes it a fixed/absolute overlay.
        var sidebarDeclarations = Regex.Matches(css, @"([^{}]*)\{([^{}]*)\}")
            .Where(match => Regex.IsMatch(match.Groups[1].Value, @"(?:\.chat-sidebar|#chat2a-sidebar)(?![-\w])"))
            .Select(match => match.Groups[2].Value)
            .ToList();
        Assert.NotEmpty(sidebarDeclarations);
        Assert.DoesNotMatch(@"position:\s*(?:fixed|absolute)", string.Join("\n", sidebarDeclarations));

        // The shared row is a flex/grid container.
        var rowSelector = string.Join("|", rowClasses.Select(Regex.Escape));
        var rowDeclarations = Regex.Matches(css, $@"([^{{}}]*\.(?:{rowSelector})(?![-\w])[^{{}}]*)\{{([^{{}}]*)\}}")
            .Select(match => match.Groups[2].Value)
            .ToList();
        Assert.Contains(rowDeclarations, declarations => Regex.IsMatch(declarations, @"display:\s*(?:flex|grid)"));

        // A 640px breakpoint (max-width 639.x or min-width 640) switches between row and stacked.
        var breakpointBodies = Regex.Matches(css, @"@media[^{]*(?:max-width:\s*639(?:\.\d+)?px|min-width:\s*640px)[^{]*\{((?:[^{}]*\{[^{}]*\})*)[^{}]*\}")
            .Select(match => match.Groups[1].Value)
            .ToList();
        Assert.Contains(breakpointBodies, body =>
            Regex.IsMatch(body, $@"\.(?:{rowSelector})(?![-\w])[^{{}}]*\{{[^{{}}]*(?:flex-direction|grid-template)")
            || Regex.IsMatch(body, @"(?:#chat2a-sidebar|\.chat-sidebar)(?![-\w])[^{}]*\{[^{}]*(?:width|height|flex)"));
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
    public void AC4_ChatSidebarScript_RefreshesThroughWeatherMapRefreshCitiesInFinally_Once()
    {
        var script = ReadRepoFile("mvc-dotnet/mvc/wwwroot/js/chatSidebar.js");

        // Some `finally` block (runs on success and on failure) refreshes the pins.
        var refreshBlock = Regex.Matches(script, @"\bfinally\s*\{")
            .Select(match => BlockAfter(script[match.Index..], "finally"))
            .FirstOrDefault(block => Regex.IsMatch(block, @"[Rr]efresh\w*\s*\("));
        Assert.True(refreshBlock is not null, "Expected a refresh call in a finally block.");

        // The finally block may inline the refresh or call a local helper.
        var refreshLogic = refreshBlock;
        var helper = Regex.Match(script, @"function\s+(\w*[Rr]efresh\w*)\s*\(");
        if (!refreshBlock.Contains("weatherMap", StringComparison.Ordinal) && helper.Success
            && refreshBlock.Contains(helper.Groups[1].Value + "(", StringComparison.Ordinal))
        {
            refreshLogic = BlockAfter(script[helper.Index..], "function");
        }

        Assert.Contains("weatherMap", refreshLogic);
        Assert.Matches(@"\.refreshCities\s*\(", refreshLogic);

        // Exactly one refresh call site: one GET /User per send, through the map's path.
        Assert.Single(Regex.Matches(script, @"\.refreshCities\s*\("));
        Assert.DoesNotContain("location.reload", script);
    }

    // AC4 (negative: the fetch('/User') fallback is gone)
    [Fact]
    public void AC4_SidebarScripts_MakeNoUserRequestOfTheirOwn()
    {
        foreach (var path in new[] { "mvc-dotnet/mvc/wwwroot/js/chatSidebar.js", "mvc-dotnet/mvc/wwwroot/js/chatRender.js" })
        {
            var script = ReadRepoFile(path);
            Assert.DoesNotMatch(@"['""`]/User['""`]", script);
        }
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

    // AC6 (the shared renderer is loaded, with markdown, before each consumer)
    [Theory]
    [InlineData("/", "js/chatSidebar.js")]
    [InlineData("/chat-clients", "js/chatClient.js")]
    public async Task AC6_PagesLoadTheSharedChatRenderer_AndMarkdownLibs_BeforeTheirChatScript(string path, string consumer)
    {
        var html = await GetHtmlAsync(path);

        var render = ScriptIndex(html, "js/chatRender.js");
        var consumerIndex = ScriptIndex(html, consumer);
        Assert.True(render < consumerIndex, $"chatRender.js must load before {consumer} on {path}.");

        foreach (var library in new[] { "js/lib/marked.min.js", "js/lib/purify.min.js", "js/markdown/safeGfmMarkdown.js" })
        {
            Assert.True(ScriptIndex(html, library) < consumerIndex, $"{library} must load before {consumer} on {path}.");
        }
    }

    // AC6 (the shared renderer owns markdown, usage chip and tool hover formatting)
    [Fact]
    public void AC6_ChatRenderScript_ExposesSharedRendering_MarkdownUsageChipAndToolHover()
    {
        var script = ReadRepoFile("mvc-dotnet/mvc/wwwroot/js/chatRender.js");

        Assert.Matches(@"window\.chatRender\s*=", script);

        // Sanitized GFM markdown through the same renderer as /chat-clients.
        Assert.Contains("safeGfmMarkdown", script);
        Assert.Contains("chat-markdown", script);

        // Usage chip: "1.2s · 345 tok" with hover/focus details.
        Assert.Contains("chat-usage-chip", script);
        Assert.Matches(@"tok[`'""]", script);
        Assert.Contains("Runtime: ", script);
        Assert.Contains("Total", script);

        // Tool hover card: Arguments / Result, "Waiting for tool output…" while running.
        Assert.Matches(@"toolDetails|data-tool-details", script);
        Assert.Contains("Arguments", script);
        Assert.Contains("Result", script);
        Assert.Contains("Waiting for tool output…", script);
        Assert.Contains("[data-tool-details]", script);
    }

    // AC6 (no second copy of the formatting logic)
    [Theory]
    [InlineData("mvc-dotnet/mvc/wwwroot/js/chatClient.js")]
    [InlineData("mvc-dotnet/mvc/wwwroot/js/chatSidebar.js")]
    public void AC6_ChatScripts_UseChatRender_WithoutTheirOwnFormatting(string path)
    {
        var script = ReadRepoFile(path);

        Assert.Contains("chatRender.", script);

        Assert.DoesNotContain("Waiting for tool output", script);
        Assert.DoesNotContain("chat-usage-chip", script);
        Assert.DoesNotContain("Runtime: ", script);
        Assert.DoesNotMatch(@"function\s+(formatChatUsageChip|formatChatUsageDetails|formatToolHoverText|formatChatRuntime|ensureToolHoverCard)\b", script);
        Assert.DoesNotMatch(@"Arguments\\n", script);
        Assert.DoesNotContain("safeGfmMarkdown.render", script);
    }

    // ---- helpers ----

    private async Task<string> GetHtmlAsync(string path)
    {
        var response = await _client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Reads a file from this checkout's root (the nearest directory holding Weather.sln), so a
    /// git worktree never falls through to the parent checkout's copy.
    /// </summary>
    private static string ReadRepoFile(string relativePath)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Weather.sln")))
        {
            dir = dir.Parent;
        }

        Assert.True(dir is not null, $"Could not find the repository root from {AppContext.BaseDirectory}.");
        var path = Path.Combine(dir!.FullName, relativePath);
        Assert.True(File.Exists(path), $"Expected {relativePath} in {dir.FullName}.");
        return File.ReadAllText(path);
    }

    private static Match PanelTag(string html)
    {
        var panelTag = Regex.Match(html, "<(\\w+)[^>]*\\bid=\"chat2a-sidebar\"[^>]*>");
        Assert.True(panelTag.Success, "Expected an element with id=\"chat2a-sidebar\".");
        return panelTag;
    }

    private static int ScriptIndex(string html, string src)
    {
        var match = Regex.Match(html, $"<script[^>]*src=\"[^\"]*{Regex.Escape(src)}[^\"]*\"");
        Assert.True(match.Success, $"Expected a <script> for {src}.");
        return match.Index;
    }

    private static readonly HashSet<string> VoidElements = new(StringComparer.OrdinalIgnoreCase)
    {
        "area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "source", "track", "wbr",
    };

    /// <summary>Opening tags of the elements enclosing <paramref name="position"/>, outermost first.</summary>
    private static List<(int Index, string Tag)> AncestorsAt(string html, int position)
    {
        var stack = new List<(int Index, string Name, string Tag)>();
        foreach (Match tag in Regex.Matches(html[..position], @"<(/?)([a-zA-Z][\w-]*)([^>]*)>"))
        {
            var name = tag.Groups[2].Value;
            if (tag.Groups[1].Value == "/")
            {
                var open = stack.FindLastIndex(entry => string.Equals(entry.Name, name, StringComparison.OrdinalIgnoreCase));
                if (open >= 0)
                {
                    stack.RemoveRange(open, stack.Count - open);
                }
            }
            else if (!VoidElements.Contains(name) && !tag.Groups[3].Value.EndsWith('/'))
            {
                stack.Add((tag.Index, name, tag.Value));
            }
        }

        return stack.Select(entry => (entry.Index, entry.Tag)).ToList();
    }

    /// <summary>The opening tag of the innermost element containing both positions.</summary>
    private static string? CommonAncestorTag(string html, int first, int second)
    {
        var a = AncestorsAt(html, first);
        var b = AncestorsAt(html, second);
        string? common = null;
        for (var i = 0; i < Math.Min(a.Count, b.Count) && a[i].Index == b[i].Index; i++)
        {
            common = a[i].Tag;
        }

        return common;
    }

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
