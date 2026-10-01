using System.Net;
using System.Text.RegularExpressions;

namespace WeatherMVC.Tests;

// Implementation details not covered by Chat5aSidebarDockAcceptanceTests: script load order,
// toggle/Escape wiring, entry fields fed to chatRender, and the docked CSS layout.
public class Chat5aSidebarLayoutTests(WeatherMvcWebApplicationFactory factory) : IClassFixture<WeatherMvcWebApplicationFactory>
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
    public async Task Home_SidebarGateRow_HasFiveGatesInOrder_WithCodeInputAndLlmOutputUnchecked()
    {
        var html = await GetHtmlAsync("/");

        var row = Regex.Match(html, "<div id=\"chat5a-sidebar-gate-options\"[^>]*>(.*?)</div>", RegexOptions.Singleline);
        Assert.True(row.Success, "Expected the sidebar gate row.");
        Assert.True(row.Index > html.IndexOf("id=\"chat5a-sidebar-input\"", StringComparison.Ordinal), "Gates must be below the textarea.");

        var inputs = Regex.Matches(row.Groups[1].Value, "<input[^>]*data-sidebar-gate=\"(\\w+)\"[^>]*>");
        Assert.Equal(["maxLength", "ruleInput", "llmInput", "systemPrompt", "llmOutput"], inputs.Select(input => input.Groups[1].Value));
        Assert.All(inputs, input => Assert.Equal(input.Groups[1].Value is not ("ruleInput" or "llmOutput"), input.Value.Contains(" checked")));

        // The /chat-clients row is read by id in chatClient.js, so the sidebar must not reuse it or its attribute.
        Assert.DoesNotContain("id=\"chat-gate-options\"", html);
        Assert.DoesNotContain("data-chat-gate=", html);
    }

    [Fact]
    public async Task SidebarGateTitles_MatchTheChatClientsGateTitles()
    {
        static List<string> Titles(string html, string container) =>
            Regex.Matches(Regex.Match(html, $"<div id=\"{container}\"[^>]*>(.*?)</div>", RegexOptions.Singleline).Groups[1].Value, "<label title=\"([^\"]*)\"")
                .Select(match => match.Groups[1].Value)
                .ToList();

        var sidebar = Titles(await GetHtmlAsync("/"), "chat5a-sidebar-gate-options");
        var panel = Titles(await GetHtmlAsync("/chat-clients"), "chat-gate-options");

        Assert.Equal(5, sidebar.Count);
        Assert.Equal(panel, sidebar);
        Assert.Contains(sidebar, title => System.Net.WebUtility.HtmlDecode(title) ==
            "Orchestration - Prompt-based Scope Guard. An instruction in the orchestrator's own system prompt telling it to only handle weather, location/geo and saved-city requests (list, add/save, remove/delete) — no code enforces this, so it's the easiest gate to bypass.");
    }

    [Fact]
    public void ChatSidebarScript_ReadsGateCheckboxes_AndRendersBlockedEntries()
    {
        var script = ReadRepoFile("mvc-dotnet/mvc/wwwroot/js/chatSidebar.js");

        Assert.Contains("document.getElementById('chat5a-sidebar-gate-options')", script);
        Assert.Contains("input[data-sidebar-gate]", script);
        Assert.Contains("enableMaxLengthGate: isGateEnabled('maxLength', true),", script);
        Assert.Contains("enableRuleInputGate: isGateEnabled('ruleInput', false),", script);
        Assert.Contains("enableLlmInputGate: isGateEnabled('llmInput', true),", script);
        Assert.Contains("enableSystemPromptGuard: isGateEnabled('systemPrompt', true),", script);
        Assert.Contains("enableLlmOutputGate: isGateEnabled('llmOutput', false),", script);
        Assert.Contains("addEntry({ role: 'blocked', content: payload.errorMessage });", script);
    }

    [Fact]
    public void ChatSidebarScript_FocusesTheInputAfterReEnablingIt_InFinally()
    {
        var script = ReadRepoFile("mvc-dotnet/mvc/wwwroot/js/chatSidebar.js");

        var finallyIndex = script.LastIndexOf("} finally {", StringComparison.Ordinal);
        Assert.True(finallyIndex >= 0);
        var block = script[finallyIndex..];

        var reEnable = block.IndexOf("isSending = false;", StringComparison.Ordinal);
        var controls = block.IndexOf("updateSendingControls();", StringComparison.Ordinal);
        var focus = block.IndexOf("input.focus();", StringComparison.Ordinal);
        Assert.True(reEnable >= 0 && reEnable < controls && controls < focus, "input.focus() must run after the textarea is re-enabled.");
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
