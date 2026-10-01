using System.Net;
using System.Text.RegularExpressions;

namespace WeatherMVC.Tests;

/// <summary>
/// Acceptance tests for docs/specs/2026-10-01-home-chat5a-sidebar-gates.md (MVC): AC1, AC2,
/// AC3, AC4, AC10. Served HTML is asserted through <see cref="WeatherMvcWebApplicationFactory"/>.
/// MVC has no JS test runner, so script behaviour is asserted on chatSidebar.js's source (the
/// same pattern as the Chat2a sidebar tests). AC5-AC9 live in core.tests.
/// </summary>
public class Chat5aSidebarGatesAcceptanceTests(WeatherMvcWebApplicationFactory factory) : IClassFixture<WeatherMvcWebApplicationFactory>
{
    private const string SidebarScriptPath = "mvc-dotnet/mvc/wwwroot/js/chatSidebar.js";

    private static readonly (string Key, string Label, string Field, bool DefaultChecked)[] Gates =
    [
        ("maxLength", "500 Char", "enableMaxLengthGate", true),
        ("ruleInput", "Code Input", "enableRuleInputGate", false),
        ("llmInput", "LLM Input", "enableLlmInputGate", true),
        ("systemPrompt", "Sys Prompt", "enableSystemPromptGuard", true),
        ("llmOutput", "LLM Output", "enableLlmOutputGate", false),
    ];

    private readonly HttpClient _client = factory.CreateClient();

    // AC1
    [Fact]
    public void AC1_SidebarScript_PostsToChat5aMessages_NotChat2a()
    {
        var script = ReadRepoFile(SidebarScriptPath);

        Assert.Matches("['\"`]/Chat5a/messages['\"`]", script);
        Assert.Matches(@"method:\s*['""]POST['""]", script);
        Assert.DoesNotContain("/Chat2a/messages", script);
        Assert.DoesNotMatch(@"/Chat(1a|1b|2a|2b|3|4a|4b|5b)/messages", script);
        Assert.Single(Regex.Matches(script, @"\bfetch\s*\("));
    }

    // AC1
    [Fact]
    public void AC1_SidebarScript_SendsSessionIdMessageAndAllFiveGateFields_FromTheSidebarCheckboxes()
    {
        var script = ReadRepoFile(SidebarScriptPath);

        Assert.Matches(@"\bsessionId(\s*:\s*\w+)?\s*,\s*message\b", script);
        foreach (var (key, _, field, _) in Gates)
        {
            // Each field is read from its own gate (for example enableRuleInputGate <- ruleInput).
            Assert.Matches($@"\b{field}\s*:[^,}}\n]*\b{key}\b", script);
        }

        Assert.Contains("data-sidebar-gate", script);
        Assert.Contains(".checked", script);

        // The sidebar must not read the /chat-clients gate row.
        Assert.DoesNotContain("chat-gate-options", script);
        Assert.DoesNotContain("data-chat-gate", script);
    }

    // AC1: sessionId is null on the first send, then the id from the stream's session event.
    [Fact]
    public void AC1_SidebarScript_StartsWithANullSession_AndAdoptsTheStreamSessionId()
    {
        var script = ReadRepoFile(SidebarScriptPath);

        Assert.Matches(@"let\s+sessionId\s*=\s*null\s*;", script);
        Assert.Matches("['\"]session['\"]", script);
        Assert.Matches(@"sessionId\s*=\s*payload\.sessionId", script);
    }

    // AC2
    [Fact]
    public async Task AC2_Home_RendersAComplementaryPanelNamedChat5a_WithAChat5aHeading()
    {
        var html = await GetHtmlAsync("/");
        var panel = SidebarHtml(html);
        var openTag = Regex.Match(panel, "^<[^>]+>").Value;

        Assert.Contains("role=\"complementary\"", openTag);
        Assert.Contains("aria-label=\"Chat5a\"", openTag);
        Assert.Matches(@"<h[1-6][^>]*>\s*Chat5a\s*</h[1-6]>", panel);

        foreach (var id in new[] { "chat5a-sidebar-input", "chat5a-sidebar-messages", "chat5a-sidebar-form", "chat5a-sidebar-send", "chat5a-sidebar-close" })
        {
            Assert.Contains($"id=\"{id}\"", panel);
        }

        Assert.DoesNotContain("chat2a-sidebar", html);
        Assert.DoesNotContain("aria-label=\"Chat2a\"", html);
    }

    // AC2
    [Fact]
    public async Task AC2_Home_FiveGateCheckboxesBelowTheTextarea_InOrder_WithSidebarDefaults()
    {
        var html = await GetHtmlAsync("/");
        var panel = SidebarHtml(html);
        var gates = GateRow(panel);

        Assert.Equal(Gates.Select(g => g.Key).ToArray(), gates.Select(g => g.Key).ToArray());
        for (var i = 0; i < Gates.Length; i++)
        {
            Assert.StartsWith(Gates[i].Label, gates[i].Label);
            Assert.True(
                Gates[i].DefaultChecked == gates[i].Checked,
                $"{Gates[i].Label} should default to {(Gates[i].DefaultChecked ? "checked" : "unchecked")} in the sidebar.");
        }

        var textarea = panel.IndexOf("<textarea", StringComparison.Ordinal);
        Assert.True(textarea >= 0, "Expected the sidebar textarea.");
        Assert.All(gates, g => Assert.True(g.Index > textarea, $"{g.Label} must be below the textarea."));
    }

    // AC2: same labels and hover descriptions as the /chat-clients Chat5a tab.
    [Fact]
    public async Task AC2_GateLabelsAndTitles_MatchTheChatClientsGateRow()
    {
        var sidebar = GateRow(SidebarHtml(await GetHtmlAsync("/")));
        var chatClients = await GetHtmlAsync("/chat-clients");
        var panelRow = Regex.Match(chatClients, "<div[^>]*id=\"chat-gate-options\"[^>]*>(.*?)</div>", RegexOptions.Singleline);
        Assert.True(panelRow.Success, "Expected the /chat-clients gate row.");
        var panel = Regex.Matches(panelRow.Groups[1].Value, "<label([^>]*)>(.*?)</label>", RegexOptions.Singleline)
            .Select(m => (Title: Decode(Attr(m.Groups[1].Value, "title")), Label: Text(m.Groups[2].Value)))
            .ToList();

        Assert.Equal(panel.Count, sidebar.Count);
        for (var i = 0; i < panel.Count; i++)
        {
            Assert.False(string.IsNullOrWhiteSpace(sidebar[i].Title), $"{sidebar[i].Label} needs a hover description.");
            Assert.Equal(panel[i].Title, sidebar[i].Title);
            // MVC may use the short "LLM Output" label; React/Blazor use the full one.
            Assert.True(
                panel[i].Label.StartsWith(sidebar[i].Label, StringComparison.Ordinal)
                    || sidebar[i].Label.StartsWith(panel[i].Label, StringComparison.Ordinal),
                $"Sidebar label \"{sidebar[i].Label}\" should match \"{panel[i].Label}\".");
        }
    }

    // AC2 (negative: the defaults change only in the sidebar)
    [Fact]
    public async Task AC2_ChatClientsGateRow_StillDefaultsAllFiveChecked()
    {
        var html = await GetHtmlAsync("/chat-clients");

        var inputs = Regex.Matches(html, "<input[^>]*data-chat-gate=\"[^\"]+\"[^>]*>");
        Assert.Equal(5, inputs.Count);
        Assert.All(inputs, m => Assert.Matches(@"\schecked(\s|>|=|/)", m.Value));
    }

    // AC2: the sidebar gate row has its own container, distinct from the /chat-clients one.
    [Fact]
    public async Task AC2_Home_SidebarGateRow_DoesNotReuseTheChatClientsIds()
    {
        var html = await GetHtmlAsync("/");

        Assert.DoesNotContain("id=\"chat-gate-options\"", html);
        Assert.DoesNotContain("data-chat-gate=", html);
    }

    // AC3
    [Fact]
    public void AC3_SidebarScript_RendersBlockedEventsAsBlockedEntries()
    {
        var script = ReadRepoFile(SidebarScriptPath);

        Assert.Matches(@"payload\.type\s*===\s*['""]blocked['""]", script);
        Assert.Matches(@"role:\s*['""]blocked['""]\s*,\s*content:\s*payload\.errorMessage", script);

        // chatRender.js (shared with /chat-clients) knows how to style the blocked role.
        Assert.Contains("'blocked'", ReadRepoFile("mvc-dotnet/mvc/wwwroot/js/chatRender.js"));
    }

    // AC3: token, tool and error events and the map refresh keep working.
    [Fact]
    public void AC3_SidebarScript_StillHandlesTokenToolErrorDone_AndRefreshesTheMapOnceInFinally()
    {
        var script = ReadRepoFile(SidebarScriptPath);

        foreach (var eventType in new[] { "token", "tool_start", "tool_end", "error", "done" })
        {
            Assert.Matches($@"payload\.type\s*===\s*['""]{eventType}['""]", script);
        }

        Assert.Single(Regex.Matches(script, @"\.refreshCities\s*\("));
        var finallyBlock = SubmitFinallyBlock(script);
        Assert.Matches(@"[Rr]efresh\w*\s*\(", finallyBlock);
    }

    // AC4
    [Fact]
    public async Task AC4_OpenChatButton_ControlsTheChat5aPanel_AndThePanelStartsHidden()
    {
        var html = await GetHtmlAsync("/");

        var button = Regex.Match(html, "<button[^>]*aria-label=\"Open chat\"[^>]*>");
        Assert.True(button.Success, "Expected the Open chat header button.");
        Assert.Contains("aria-controls=\"chat5a-sidebar\"", button.Value);
        Assert.Contains("aria-expanded=\"false\"", button.Value);
        Assert.True(button.Index < html.IndexOf("</header>", StringComparison.Ordinal));

        var panelTag = Regex.Match(SidebarHtml(html), "^<[^>]+>").Value;
        Assert.Matches(@"\shidden(\s|>|=)", panelTag);
        Assert.True(html.IndexOf("id=\"chat5a-sidebar\"", StringComparison.Ordinal) > html.IndexOf("</header>", StringComparison.Ordinal));
    }

    // AC4
    [Fact]
    public void AC4_SidebarScript_StillTogglesClosesAndHandlesEscape()
    {
        var script = ReadRepoFile(SidebarScriptPath);

        foreach (var id in new[] { "chatSidebarButton", "chat5a-sidebar", "chat5a-sidebar-close", "chat5a-sidebar-input", "chat5a-sidebar-form" })
        {
            Assert.Matches($"['\"]{id}['\"]", script);
        }

        Assert.Contains("aria-expanded", script);
        Assert.Matches(@"\.hidden\s*=", script);
        Assert.Matches("['\"]Escape['\"]", script);
    }

    // AC4 (negative: no sidebar off the map page)
    [Theory]
    [InlineData("/hello-world")]
    [InlineData("/chat-clients")]
    public async Task AC4_OtherRoutes_HaveNoOpenChatButtonOrSidebar(string path)
    {
        var html = await GetHtmlAsync(path);

        Assert.DoesNotContain("aria-label=\"Open chat\"", html);
        Assert.DoesNotContain("id=\"chat5a-sidebar\"", html);
        Assert.DoesNotContain("js/chatSidebar.js", html);
    }

    // AC10: after every completion (normal, blocked, or failed) the textarea is re-enabled, then focused.
    [Fact]
    public void AC10_SubmitFinally_ReenablesTheTextareaBeforeFocusingIt()
    {
        var script = ReadRepoFile(SidebarScriptPath);
        var finallyBlock = SubmitFinallyBlock(script);

        var clearSending = Regex.Match(finallyBlock, @"isSending\s*=\s*false");
        var updateControls = Regex.Match(finallyBlock, @"updateSendingControls\s*\(\s*\)");
        var focus = Regex.Match(finallyBlock, @"\binput\.focus\s*\(\s*\)");

        Assert.True(clearSending.Success, "finally must clear isSending.");
        Assert.True(updateControls.Success, "finally must re-run updateSendingControls().");
        Assert.True(focus.Success, "finally must focus the textarea.");
        Assert.True(clearSending.Index < updateControls.Index, "isSending must be cleared before the controls update.");
        Assert.True(updateControls.Index < focus.Index, "The textarea must be enabled before it is focused (a disabled element cannot take focus).");

        // updateSendingControls is what re-enables the textarea.
        var update = BlockAfter(script, "function updateSendingControls(");
        Assert.Matches(@"input\.disabled\s*=\s*isSending", update);
    }

    // AC10 (edge: a failed request still reaches the finally block, no early return skips focus)
    [Fact]
    public void AC10_FailedOrBlockedSends_StillReachTheFocusInFinally()
    {
        var script = ReadRepoFile(SidebarScriptPath);
        var submit = BlockAfter(script, "form.addEventListener('submit'");

        Assert.Matches(@"\btry\s*\{", submit);
        Assert.Matches(@"\bcatch\s*(\([^)]*\))?\s*\{", submit);
        var focusInFinally = Regex.IsMatch(SubmitFinallyBlock(script), @"\binput\.focus\s*\(");
        Assert.True(focusInFinally);

        // Errors (HTTP failures, network failures) are swallowed into an error entry, not rethrown.
        var catchBlock = BlockAfter(submit[Regex.Match(submit, @"\bcatch\b").Index..], "catch");
        Assert.DoesNotMatch(@"\bthrow\b", catchBlock);
    }

    // ---- helpers ----

    private sealed record GateInfo(string Key, string Label, string Title, bool Checked, int Index);

    private async Task<string> GetHtmlAsync(string path)
    {
        var response = await _client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadAsStringAsync();
    }

    private static string ReadRepoFile(string relativePath) => File.ReadAllText(RepoFiles.FindRepoFile(relativePath));

    /// <summary>The sidebar element's HTML, from its opening tag to its matching close tag.</summary>
    private static string SidebarHtml(string html)
    {
        var open = Regex.Match(html, "<(\\w+)[^>]*\\bid=\"chat5a-sidebar\"[^>]*>");
        Assert.True(open.Success, "Expected an element with id=\"chat5a-sidebar\".");
        var name = open.Groups[1].Value;
        var depth = 0;
        foreach (Match tag in Regex.Matches(html[open.Index..], $"<(/?){name}\\b[^>]*>"))
        {
            depth += tag.Groups[1].Value == "/" ? -1 : 1;
            if (depth == 0)
            {
                return html.Substring(open.Index, tag.Index + tag.Length);
            }
        }

        return html[open.Index..];
    }

    private static List<GateInfo> GateRow(string panel)
        => Regex.Matches(panel, "<label([^>]*)>(.*?)</label>", RegexOptions.Singleline)
            .Select(label => (label, input: Regex.Match(label.Groups[2].Value, "<input[^>]*data-sidebar-gate=\"([^\"]+)\"[^>]*>")))
            .Where(pair => pair.input.Success)
            .Select(pair =>
            {
                Assert.Matches("type=\"checkbox\"", pair.input.Value);
                return new GateInfo(
                    pair.input.Groups[1].Value,
                    Text(pair.label.Groups[2].Value),
                    Decode(Attr(pair.label.Groups[1].Value, "title")),
                    Regex.IsMatch(pair.input.Value, @"\schecked(\s|>|=|/)"),
                    pair.label.Index);
            })
            .ToList();

    private static string Attr(string attributes, string name)
    {
        var match = Regex.Match(attributes, $"\\b{name}=\"([^\"]*)\"");
        return match.Success ? match.Groups[1].Value : string.Empty;
    }

    private static string Text(string html)
        => Decode(Regex.Replace(Regex.Replace(html, "<[^>]+>", " "), @"\s+", " ").Trim());

    private static string Decode(string value) => WebUtility.HtmlDecode(value);

    /// <summary>The finally block of the sidebar form's submit handler.</summary>
    private static string SubmitFinallyBlock(string script)
    {
        var submit = BlockAfter(script, "form.addEventListener('submit'");
        var finallyMatch = Regex.Match(submit, @"\bfinally\s*\{");
        Assert.True(finallyMatch.Success, "Expected a finally block in the submit handler.");
        return BlockAfter(submit[finallyMatch.Index..], "finally");
    }

    /// <summary>Returns the brace-balanced block that follows the first occurrence of <paramref name="marker"/>.</summary>
    private static string BlockAfter(string script, string marker)
    {
        var start = script.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Expected to find {marker}.");
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
