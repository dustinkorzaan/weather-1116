using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.FluentUI.AspNetCore.Components;
using WeatherBlazor.Data;
using WeatherBlazor.Shared;

namespace WeatherBlazor.Tests;

/// <summary>
/// Acceptance tests for docs/specs/2026-10-01-home-chat5a-sidebar-gates.md (Blazor):
/// AC1, AC2, AC3, AC4, AC10. AC5-AC9 live in core.tests (Chat5GateScopeAcceptanceTests).
/// </summary>
public sealed class Chat5aSidebarGatesAcceptanceTests
{
    private const string FocusIdentifier = "Blazor._internal.domWrapper.focus";

    private static readonly string[] GateLabels = ["500 Char", "Code Input", "LLM Input", "Sys Prompt", "LLM Output"];

    // AC1
    [Fact]
    public void AC1_Sends_PostOnceEachToChat5a_WithDefaultGates_AndTheStreamSessionId()
    {
        var handler = new ScriptedChatHandler(
            ScriptedChatHandler.Sse(
                new { type = "session", sessionId = "sidebar-5a-1" },
                new { type = "token", text = "first" },
                new { type = "done" }),
            ScriptedChatHandler.Sse(new { type = "token", text = "second" }, new { type = "done" }));
        using var context = CreateContext(handler);
        var composer = new ComposerValue(context);
        var rendered = RenderSidebar(context);

        Send(rendered, composer, "Add Nashville");
        rendered.WaitForAssertion(() => Assert.Contains("first", rendered.Find("#chat5a-sidebar-messages").TextContent));

        Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.EndsWith("/Chat5a/messages", handler.Requests[0].Path);
        AssertBody(handler.Requests[0].Body, null, "Add Nashville", ruleInput: false);

        Send(rendered, composer, "Remove Denver");
        rendered.WaitForAssertion(() => Assert.Contains("second", rendered.Find("#chat5a-sidebar-messages").TextContent));

        Assert.Equal(2, handler.Requests.Count);
        Assert.EndsWith("/Chat5a/messages", handler.Requests[1].Path);
        AssertBody(handler.Requests[1].Body, "sidebar-5a-1", "Remove Denver", ruleInput: false);
    }

    // AC1 (negative)
    [Fact]
    public void AC1_Sidebar_NeverPostsToChat2a()
    {
        var handler = new ScriptedChatHandler(
            ScriptedChatHandler.Sse(new { type = "token", text = "ok" }, new { type = "done" }));
        using var context = CreateContext(handler);
        var composer = new ComposerValue(context);
        var rendered = RenderSidebar(context);

        Send(rendered, composer, "hello");
        rendered.WaitForAssertion(() => Assert.Contains("ok", rendered.Find("#chat5a-sidebar-messages").TextContent));

        Assert.DoesNotContain(handler.Requests, r => r.Path.EndsWith("/Chat2a/messages", StringComparison.Ordinal));
        Assert.All(handler.Requests, r => Assert.EndsWith("/Chat5a/messages", r.Path));
    }

    // AC2
    [Fact]
    public void AC2_Panel_IsComplementaryNamedChat5a_WithChat5aHeading()
    {
        using var context = CreateContext(new ScriptedChatHandler());
        var rendered = RenderSidebar(context);

        var aside = rendered.Find("#chat5a-sidebar");
        Assert.Equal("complementary", aside.GetAttribute("role"));
        Assert.Equal("Chat5a", aside.GetAttribute("aria-label"));
        Assert.Contains(aside.QuerySelectorAll("h1,h2,h3,h4"), h => h.TextContent.Trim() == "Chat5a");
        Assert.NotNull(rendered.Find("#chat5a-sidebar-input"));
        Assert.Empty(rendered.FindAll("#chat2a-sidebar"));
    }

    // AC2
    [Fact]
    public void AC2_FiveGateCheckboxes_BelowTheTextarea_InOrder_CodeInputAndLlmOutputUnchecked()
    {
        using var context = CreateContext(new ScriptedChatHandler());
        var rendered = RenderSidebar(context);

        var gates = GateRow(rendered.Find("#chat5a-sidebar"));
        Assert.Equal(5, gates.Count);
        for (var i = 0; i < GateLabels.Length; i++)
        {
            Assert.StartsWith(GateLabels[i], gates[i].Label);
        }

        Assert.Equal(new[] { true, false, true, true, false }, gates.Select(g => g.Checked).ToArray());

        // Below the textarea: every checkbox comes after it in document order.
        var all = rendered.Find("#chat5a-sidebar").QuerySelectorAll("textarea, input[type=checkbox]").ToList();
        Assert.Equal("TEXTAREA", all[0].TagName, ignoreCase: true);
    }

    // AC2: same labels and hover descriptions as the /chat-clients Chat5a tab.
    [Fact]
    public void AC2_GateLabelsAndTitles_MatchTheChatClientsChat5aTab()
    {
        using var sidebarContext = CreateContext(new ScriptedChatHandler());
        var sidebar = RenderSidebar(sidebarContext);
        var sidebarGates = GateRow(sidebar.Find("#chat5a-sidebar"));
        Assert.All(sidebarGates, g => Assert.False(string.IsNullOrWhiteSpace(g.Title)));

        using var panelContext = CreateContext(new ScriptedChatHandler());
        var panel = panelContext.Render<ChatPanel>();
        SelectChatPanelTab(panel, "Chat5a");
        var panelGates = GateRow(panel.Find(".chat-gate-options"));

        Assert.Equal(
            panelGates.Select(g => (g.Label, g.Title)).ToArray(),
            sidebarGates.Select(g => (g.Label, g.Title)).ToArray());
    }

    // AC2 (negative: the Code Input / LLM Output defaults change only in the sidebar)
    [Fact]
    public void AC2_ChatClientsChat5aTab_StillDefaultsAllFiveGatesChecked()
    {
        using var context = CreateContext(new ScriptedChatHandler());
        var panel = context.Render<ChatPanel>();
        SelectChatPanelTab(panel, "Chat5a");

        Assert.All(GateRow(panel.Find(".chat-gate-options")), g => Assert.True(g.Checked, g.Label));
    }

    // AC2: toggling changes the matching field on the next send.
    [Fact]
    public void AC2_TogglingGates_ChangesTheMatchingFieldsOnTheNextSend()
    {
        var handler = new ScriptedChatHandler(
            ScriptedChatHandler.Sse(new { type = "token", text = "one" }, new { type = "done" }),
            ScriptedChatHandler.Sse(new { type = "token", text = "two" }, new { type = "done" }));
        using var context = CreateContext(handler);
        var composer = new ComposerValue(context);
        var rendered = RenderSidebar(context);

        GateCheckbox(rendered, "Code Input").Change(true);
        Send(rendered, composer, "Save Denver");
        rendered.WaitForAssertion(() => Assert.Contains("one", rendered.Find("#chat5a-sidebar-messages").TextContent));
        AssertBody(handler.Requests[0].Body, null, "Save Denver", ruleInput: true);

        GateCheckbox(rendered, "LLM Output").Change(true);
        GateCheckbox(rendered, "LLM Input").Change(false);
        Send(rendered, composer, "Pin Seattle");
        rendered.WaitForAssertion(() => Assert.Contains("two", rendered.Find("#chat5a-sidebar-messages").TextContent));
        AssertBody(handler.Requests[1].Body, null, "Pin Seattle", ruleInput: true, llmOutput: true, llmInput: false);
    }

    // AC2: gate state survives close and reopen like the conversation does.
    [Fact]
    public void AC2_GateState_SurvivesCloseAndReopen()
    {
        using var context = CreateContext(new ScriptedChatHandler());
        var rendered = RenderSidebar(context);

        GateCheckbox(rendered, "Code Input").Change(true);
        rendered.Render(parameters => parameters.Add(sidebar => sidebar.Open, false));
        rendered.Render(parameters => parameters.Add(sidebar => sidebar.Open, true));

        Assert.True(GateCheckbox(rendered, "Code Input").HasAttribute("checked"));
    }

    // AC3
    [Fact]
    public void AC3_BlockedEvent_RendersAsABlockedEntry_NotAnError()
    {
        var handler = new ScriptedChatHandler(
            ScriptedChatHandler.Sse(
                new { type = "blocked", errorMessage = "Blocked by LLM Input: message is out of scope" },
                new { type = "done" }));
        using var context = CreateContext(handler);
        var composer = new ComposerValue(context);
        var rendered = RenderSidebar(context);

        Send(rendered, composer, "Tell me a joke.");

        rendered.WaitForAssertion(() =>
        {
            var entry = rendered.Find("#chat5a-sidebar .chat-message.blocked");
            Assert.Contains("Blocked by LLM Input: message is out of scope", entry.TextContent);
            Assert.DoesNotContain("error", entry.ClassList);
        });
    }

    // AC3: the other events keep working and the map is refreshed once per completed send.
    [Fact]
    public void AC3_TokenToolErrorAndBlocked_AllRender_AndEachSendRefreshesTheMapOnce()
    {
        var handler = new ScriptedChatHandler(
            ScriptedChatHandler.Sse(
                new { type = "session", sessionId = "s1" },
                new { type = "tool_start", toolName = "AddUserCity" },
                new { type = "tool_end", toolName = "AddUserCity", toolResult = "{}" },
                new { type = "token", text = "Saved Nashville " },
                new { type = "token", text = "to your cities." },
                new { type = "done" }),
            ScriptedChatHandler.Sse(new { type = "error", errorMessage = "Chat5a failed upstream." }, new { type = "done" }),
            ScriptedChatHandler.Sse(new { type = "blocked", errorMessage = "Blocked by 500 Char" }, new { type = "done" }));
        using var context = CreateContext(handler);
        var composer = new ComposerValue(context);
        var rendered = RenderSidebar(context);

        Send(rendered, composer, "Add Nashville");
        rendered.WaitForAssertion(() =>
        {
            Assert.Contains("Saved Nashville to your cities.", rendered.Find("#chat5a-sidebar-messages").TextContent);
            Assert.Contains("AddUserCity", rendered.Find("#chat5a-sidebar-messages").TextContent);
        });
        rendered.WaitForAssertion(() => context.JSInterop.VerifyInvoke("weatherMap.refreshCities", 1));

        Send(rendered, composer, "again");
        rendered.WaitForAssertion(() =>
            Assert.Contains("Chat5a failed upstream.", rendered.Find("#chat5a-sidebar .chat-message.error").TextContent));
        rendered.WaitForAssertion(() => context.JSInterop.VerifyInvoke("weatherMap.refreshCities", 2));

        Send(rendered, composer, "blocked one");
        rendered.WaitForAssertion(() =>
            Assert.Contains("Blocked by 500 Char", rendered.Find("#chat5a-sidebar .chat-message.blocked").TextContent));
        rendered.WaitForAssertion(() => context.JSInterop.VerifyInvoke("weatherMap.refreshCities", 3));

        Assert.Equal(3, handler.Requests.Count);
        Assert.Equal(3, context.JSInterop.Invocations["weatherMap.refreshCities"].Count);
    }

    // AC4
    [Fact]
    public void AC4_OpenChatButton_TogglesTheChat5aPanel_WithAriaControlsAndExpanded()
    {
        using var context = CreateContext(new ScriptedChatHandler());
        var rendered = RenderLayout(context);

        var button = OpenChatButton(rendered);
        Assert.Equal("chat5a-sidebar", button.GetAttribute("aria-controls"));
        Assert.Equal("false", button.GetAttribute("aria-expanded"));
        Assert.True(rendered.Find("#chat5a-sidebar").HasAttribute("hidden"));

        OpenChatButton(rendered).Click();
        rendered.WaitForAssertion(() =>
        {
            Assert.False(rendered.Find("#chat5a-sidebar").HasAttribute("hidden"));
            Assert.Equal("true", OpenChatButton(rendered).GetAttribute("aria-expanded"));
        });
        Assert.Empty(rendered.FindAll(".header-actions #chat5a-sidebar"));

        OpenChatButton(rendered).Click();
        rendered.WaitForAssertion(() => Assert.True(rendered.Find("#chat5a-sidebar").HasAttribute("hidden")));
    }

    // AC4
    [Fact]
    public void AC4_CloseChatAndEscape_BothHideThePanel()
    {
        using var context = CreateContext(new ScriptedChatHandler());
        var rendered = RenderLayout(context);

        OpenChatButton(rendered).Click();
        rendered.WaitForAssertion(() => Assert.False(rendered.Find("#chat5a-sidebar").HasAttribute("hidden")));
        rendered.Find("#chat5a-sidebar button[aria-label=\"Close chat\"]").Click();
        rendered.WaitForAssertion(() =>
        {
            Assert.True(rendered.Find("#chat5a-sidebar").HasAttribute("hidden"));
            Assert.Equal("false", OpenChatButton(rendered).GetAttribute("aria-expanded"));
        });

        OpenChatButton(rendered).Click();
        rendered.WaitForAssertion(() => Assert.False(rendered.Find("#chat5a-sidebar").HasAttribute("hidden")));
        try
        {
            rendered.Find("#chat5a-sidebar").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Escape" });
        }
        catch (MissingEventHandlerException)
        {
            rendered.Find("#chat5a-sidebar textarea").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Escape" });
        }

        rendered.WaitForAssertion(() =>
        {
            Assert.True(rendered.Find("#chat5a-sidebar").HasAttribute("hidden"));
            Assert.Equal("false", OpenChatButton(rendered).GetAttribute("aria-expanded"));
        });
    }

    // AC4 (negative: the button exists only on the map page)
    [Theory]
    [InlineData("/hello-world")]
    [InlineData("/chat-clients")]
    public void AC4_OtherRoutes_HaveNoOpenChatButton(string route)
    {
        using var context = CreateContext(new ScriptedChatHandler());
        var rendered = RenderLayout(context);

        context.Services.GetRequiredService<NavigationManager>().NavigateTo(route);

        rendered.WaitForAssertion(() => Assert.Empty(rendered.FindAll("button[aria-label=\"Open chat\"]")));
    }

    public static TheoryData<string> AC10Outcomes => new() { "done", "blocked", "http-error", "throws" };

    // AC10
    [Theory]
    [MemberData(nameof(AC10Outcomes))]
    public void AC10_AfterASendCompletes_TheTextareaIsEnabledAndFocused(string outcome)
    {
        Func<HttpResponseMessage> reply = outcome switch
        {
            "done" => ScriptedChatHandler.Sse(new { type = "token", text = "Saved." }, new { type = "done" }),
            "blocked" => ScriptedChatHandler.Sse(new { type = "blocked", errorMessage = "Blocked by Code Input" }, new { type = "done" }),
            "http-error" => ScriptedChatHandler.Failure(HttpStatusCode.InternalServerError),
            _ => ScriptedChatHandler.Throws(),
        };
        var handler = new ScriptedChatHandler(reply);
        using var context = CreateContext(handler);
        var composer = new ComposerValue(context);
        var rendered = RenderSidebar(context);
        var focusCallsBeforeSend = FocusCalls(context).Count;

        Send(rendered, composer, "Add Nashville");

        rendered.WaitForAssertion(() => Assert.Single(handler.Requests));
        rendered.WaitForAssertion(() =>
        {
            Assert.False(rendered.Find("#chat5a-sidebar-input").HasAttribute("disabled"));
            Assert.True(
                FocusCalls(context).Count > focusCallsBeforeSend,
                "The sidebar textarea must be focused again after the send completes.");
        });
        AssertLastFocusTargetsTheTextarea(rendered, context);
    }

    // AC10: holds for every send, not just the first.
    [Fact]
    public void AC10_FocusIsRestoredAfterEachOfSeveralSends()
    {
        var handler = new ScriptedChatHandler(
            ScriptedChatHandler.Sse(new { type = "token", text = "r1" }, new { type = "done" }),
            ScriptedChatHandler.Sse(new { type = "token", text = "r2" }, new { type = "done" }));
        using var context = CreateContext(handler);
        var composer = new ComposerValue(context);
        var rendered = RenderSidebar(context);
        var baseline = FocusCalls(context).Count;

        Send(rendered, composer, "one");
        rendered.WaitForAssertion(() => Assert.Contains("r1", rendered.Find("#chat5a-sidebar-messages").TextContent));
        rendered.WaitForAssertion(() => Assert.True(FocusCalls(context).Count >= baseline + 1));
        var afterFirst = FocusCalls(context).Count;

        Send(rendered, composer, "two");
        rendered.WaitForAssertion(() => Assert.Contains("r2", rendered.Find("#chat5a-sidebar-messages").TextContent));
        rendered.WaitForAssertion(() => Assert.True(FocusCalls(context).Count >= afterFirst + 1));
        Assert.False(rendered.Find("#chat5a-sidebar-input").HasAttribute("disabled"));
    }

    // ---- helpers ----

    private sealed record Gate(string Label, string Title, bool Checked);

    private static List<Gate> GateRow(AngleSharp.Dom.IElement container)
        => container.QuerySelectorAll("input[type=checkbox]")
            .Select(input =>
            {
                var label = input.Closest("label");
                return new Gate(
                    Collapse(label?.TextContent ?? string.Empty),
                    label?.GetAttribute("title") ?? input.GetAttribute("title") ?? string.Empty,
                    input.HasAttribute("checked"));
            })
            .ToList();

    private static AngleSharp.Dom.IElement GateCheckbox(IRenderedComponent<Chat5aSidebar> rendered, string labelPrefix)
        => rendered.Find("#chat5a-sidebar").QuerySelectorAll("input[type=checkbox]")
            .Single(input => Collapse(input.Closest("label")?.TextContent ?? string.Empty)
                .StartsWith(labelPrefix, StringComparison.Ordinal));

    private static string Collapse(string text) => string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static void SelectChatPanelTab(IRenderedComponent<ChatPanel> panel, string tabId)
    {
        // FluentTabs switching needs the web component; set the bound ActiveTab directly.
        var property = typeof(ChatPanel).GetProperty("ActiveTab", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(property);
        panel.InvokeAsync(() => property!.SetValue(panel.Instance, tabId)).GetAwaiter().GetResult();
        panel.Render();
        panel.WaitForAssertion(() => Assert.NotNull(panel.Find(".chat-gate-options")));
    }

    private static List<JSRuntimeInvocation> FocusCalls(BunitContext context)
        => context.JSInterop.Invocations.Where(i => i.Identifier == FocusIdentifier).ToList();

    private static void AssertLastFocusTargetsTheTextarea(IRenderedComponent<Chat5aSidebar> rendered, BunitContext context)
    {
        var last = FocusCalls(context).Last();
        if (last.Arguments.Count == 0 || last.Arguments[0] is not ElementReference reference)
        {
            return;
        }

        var textarea = rendered.Find("#chat5a-sidebar-input");
        var refAttribute = textarea.Attributes.FirstOrDefault(a =>
            a.Name.Equals("blazor:elementreference", StringComparison.OrdinalIgnoreCase));
        if (refAttribute is not null)
        {
            Assert.Equal(refAttribute.Value, reference.Id);
        }
    }

    private static BunitContext CreateContext(HttpMessageHandler chatHandler)
    {
        var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.Services.AddHttpClient();
        context.Services.AddFluentUIComponents();
        context.Services.AddSingleton<IConfiguration>(
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["API_DOTNET_URL"] = "http://localhost:8080",
                    ["UI_REACT_URL"] = "http://localhost:3000",
                    ["MVC_URL"] = "http://localhost:8100",
                    ["WORKER_DOTNET_URL"] = "http://localhost:8130",
                })
                .Build());
        context.Services.AddSingleton(
            new WeatherApiClient(new HttpClient { BaseAddress = new Uri("http://localhost/") }, NullLogger<WeatherApiClient>.Instance));
        context.Services.AddSingleton(
            new ChatApiClient(new HttpClient(chatHandler) { BaseAddress = new Uri("http://localhost:8080/") }));
        return context;
    }

    private static IRenderedComponent<MainLayout> RenderLayout(BunitContext context)
        => context.Render<MainLayout>(parameters => parameters.Add(layout => layout.Body, "<div id=\"child\"></div>"));

    private static IRenderedComponent<Chat5aSidebar> RenderSidebar(BunitContext context)
        => context.Render<Chat5aSidebar>(parameters => parameters
            .Add(sidebar => sidebar.Open, true)
            .Add(sidebar => sidebar.OnClose, () => { }));

    private static AngleSharp.Dom.IElement OpenChatButton(IRenderedComponent<MainLayout> rendered)
        => rendered.Find(".header-actions button[aria-label=\"Open chat\"]");

    /// <summary>Types into the sidebar composer and submits (uncontrolled or bound composer).</summary>
    private static void Send(IRenderedComponent<Chat5aSidebar> rendered, ComposerValue composer, string message)
    {
        rendered.WaitForAssertion(() => Assert.False(rendered.Find("#chat5a-sidebar-input").HasAttribute("disabled")));
        composer.Value = message;
        var textarea = rendered.Find("#chat5a-sidebar-input");
        try
        {
            textarea.Input(message);
        }
        catch (MissingEventHandlerException)
        {
            try
            {
                textarea.Change(message);
            }
            catch (MissingEventHandlerException)
            {
                // Uncontrolled composer: the value comes from chatInput.getValue.
            }
        }

        rendered.Find("#chat5a-sidebar form").Submit();
    }

    private static void AssertBody(
        string body,
        string? expectedSessionId,
        string expectedMessage,
        bool ruleInput,
        bool llmOutput = false,
        bool llmInput = true)
    {
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        var names = root.EnumerateObject().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray();
        Assert.Equal(
            new[]
            {
                "enableLlmInputGate", "enableLlmOutputGate", "enableMaxLengthGate", "enableRuleInputGate",
                "enableSystemPromptGuard", "message", "sessionId",
            },
            names);
        Assert.Equal(expectedMessage, root.GetProperty("message").GetString());
        if (expectedSessionId is null)
        {
            Assert.Equal(JsonValueKind.Null, root.GetProperty("sessionId").ValueKind);
        }
        else
        {
            Assert.Equal(expectedSessionId, root.GetProperty("sessionId").GetString());
        }

        Assert.True(root.GetProperty("enableMaxLengthGate").GetBoolean());
        Assert.Equal(ruleInput, root.GetProperty("enableRuleInputGate").GetBoolean());
        Assert.Equal(llmInput, root.GetProperty("enableLlmInputGate").GetBoolean());
        Assert.True(root.GetProperty("enableSystemPromptGuard").GetBoolean());
        Assert.Equal(llmOutput, root.GetProperty("enableLlmOutputGate").GetBoolean());
    }

    /// <summary>Feeds the uncontrolled-composer JS read (chatInput.getValue) a settable value.</summary>
    private sealed class ComposerValue(BunitContext context)
    {
        private readonly BunitJSInterop _interop = context.JSInterop;

        public string Value
        {
            set => _interop.Setup<string>("chatInput.getValue", _ => true).SetResult(value);
        }
    }

    private sealed record CapturedRequest(HttpMethod Method, string Path, string Body);

    private sealed class ScriptedChatHandler(params Func<HttpResponseMessage>[] replies) : HttpMessageHandler
    {
        private readonly Queue<Func<HttpResponseMessage>> _replies = new(replies);

        public List<CapturedRequest> Requests { get; } = [];

        public static Func<HttpResponseMessage> Sse(params object[] events) => () =>
        {
            var builder = new StringBuilder();
            foreach (var streamEvent in events)
            {
                builder.Append("data: ").Append(JsonSerializer.Serialize(streamEvent)).Append("\n\n");
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(builder.ToString(), Encoding.UTF8, "text/event-stream"),
            };
        };

        public static Func<HttpResponseMessage> Throws()
            => () => throw new HttpRequestException("Network down");

        public static Func<HttpResponseMessage> Failure(HttpStatusCode status)
            => () => new HttpResponseMessage(status) { Content = new StringContent("{\"error\":\"boom\"}") };

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add(new CapturedRequest(request.Method, request.RequestUri?.AbsolutePath ?? string.Empty, body));
            var reply = _replies.Count > 0 ? _replies.Dequeue() : Sse(new { type = "done" });
            return reply();
        }
    }
}
