using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.FluentUI.AspNetCore.Components;
using WeatherBlazor.Data;
using WeatherBlazor.Markdown;
using WeatherBlazor.Shared;

namespace WeatherBlazor.Tests;

/// <summary>Acceptance tests for docs/specs/2026-09-30-chat2a-sidebar.md (Blazor).</summary>
public sealed class Chat2aSidebarAcceptanceTests
{
    // AC1
    [Fact]
    public void AC1_HomeHeaderActions_AreAddLocation_ThenOpenChat_ThenAvatar()
    {
        using var context = CreateContext(new ScriptedChatHandler());
        var rendered = RenderLayout(context);

        Assert.Equal(
            new[] { "Add location", "Open chat", "Open user menu" },
            HeaderButtonLabels(rendered));

        var chatButton = rendered.Find(".header-actions button[aria-label=\"Open chat\"]");
        Assert.Equal("chat2a-sidebar", chatButton.GetAttribute("aria-controls"));
    }

    // AC1 (negative: the chat button exists only on the map page)
    [Theory]
    [InlineData("/hello-world")]
    [InlineData("/current-ai-weather")]
    [InlineData("/chat-clients")]
    public void AC1_OnEveryOtherRoute_ThereIsNoOpenChatButton(string route)
    {
        using var context = CreateContext(new ScriptedChatHandler());
        var rendered = RenderLayout(context);

        context.Services.GetRequiredService<NavigationManager>().NavigateTo(route);

        rendered.WaitForAssertion(() =>
        {
            Assert.Empty(rendered.FindAll("button[aria-label=\"Open chat\"]"));
            Assert.Equal(new[] { "Open user menu" }, HeaderButtonLabels(rendered));
        });
    }

    // AC2
    [Fact]
    public void AC2_OpenChatTogglesChat2aComplementaryPanel_AndAriaExpanded()
    {
        using var context = CreateContext(new ScriptedChatHandler());
        var rendered = RenderLayout(context);

        var aside = rendered.Find("#chat2a-sidebar");
        Assert.Equal("Chat2a", aside.GetAttribute("aria-label"));
        Assert.Equal("complementary", aside.GetAttribute("role"));
        Assert.True(aside.HasAttribute("hidden"), "Sidebar must start hidden.");
        Assert.Equal("false", OpenChatButton(rendered).GetAttribute("aria-expanded"));

        OpenChatButton(rendered).Click();

        rendered.WaitForAssertion(() =>
        {
            Assert.False(rendered.Find("#chat2a-sidebar").HasAttribute("hidden"));
            Assert.Equal("true", OpenChatButton(rendered).GetAttribute("aria-expanded"));
        });

        // The panel is not inside the header.
        Assert.Empty(rendered.FindAll(".header-actions #chat2a-sidebar"));

        // Clicking the toggle again hides it.
        OpenChatButton(rendered).Click();

        rendered.WaitForAssertion(() =>
        {
            Assert.True(rendered.Find("#chat2a-sidebar").HasAttribute("hidden"));
            Assert.Equal("false", OpenChatButton(rendered).GetAttribute("aria-expanded"));
        });
    }

    // AC2 (Close chat button)
    [Fact]
    public void AC2_CloseChatButtonHidesPanel()
    {
        using var context = CreateContext(new ScriptedChatHandler());
        var rendered = RenderLayout(context);

        OpenChatButton(rendered).Click();
        rendered.WaitForAssertion(() => Assert.False(rendered.Find("#chat2a-sidebar").HasAttribute("hidden")));

        rendered.Find("#chat2a-sidebar button[aria-label=\"Close chat\"]").Click();

        rendered.WaitForAssertion(() =>
        {
            Assert.True(rendered.Find("#chat2a-sidebar").HasAttribute("hidden"));
            Assert.Equal("false", OpenChatButton(rendered).GetAttribute("aria-expanded"));
        });
    }

    // AC2 (Escape)
    [Fact]
    public void AC2_EscapeHidesPanel()
    {
        using var context = CreateContext(new ScriptedChatHandler());
        var rendered = RenderLayout(context);

        OpenChatButton(rendered).Click();
        rendered.WaitForAssertion(() => Assert.False(rendered.Find("#chat2a-sidebar").HasAttribute("hidden")));

        PressEscape(rendered);

        rendered.WaitForAssertion(() =>
        {
            Assert.True(rendered.Find("#chat2a-sidebar").HasAttribute("hidden"));
            Assert.Equal("false", OpenChatButton(rendered).GetAttribute("aria-expanded"));
        });
    }

    // AC2 (docked beside the map in the layout, not inside the header)
    [Fact]
    public void AC2_OpenPanelIsDockedAfterThePageBody_InTheSameRow_OutsideTheHeader()
    {
        using var context = CreateContext(new ScriptedChatHandler());
        var rendered = RenderLayout(context);

        OpenChatButton(rendered).Click();
        rendered.WaitForAssertion(() => Assert.False(rendered.Find("#chat2a-sidebar").HasAttribute("hidden")));

        var body = rendered.Find("#child");
        var aside = rendered.Find("#chat2a-sidebar");
        var header = rendered.Find(".header-actions");

        // Map (page body) first, then the panel.
        // Document order by index: AngleSharp's CompareDocumentPosition misreports
        // nodes that have different parents.
        var orderedIds = rendered.FindAll("[id]").Select(element => element.Id).ToList();
        Assert.True(
            orderedIds.IndexOf("child") >= 0
                && orderedIds.IndexOf("child") < orderedIds.IndexOf("chat2a-sidebar"),
            "The sidebar must come after the page body (map) in the layout.");

        // The closest container shared with the page body is a layout row, not the whole shell
        // with the header in it.
        var row = CommonAncestor(body, aside);
        Assert.NotNull(row);
        Assert.False(row!.Contains(header), "The sidebar must share a row with the map, below the header.");
        Assert.False(string.IsNullOrWhiteSpace(row.ClassName), "The map/sidebar row needs a class to style it.");
    }

    // AC2 (not a fixed overlay; row at >= 640px, stacked below)
    [Fact]
    public void AC2_SiteCss_DocksSidebarInFlow_RowAt640_StackedBelow()
    {
        using var context = CreateContext(new ScriptedChatHandler());
        var rendered = RenderLayout(context);
        OpenChatButton(rendered).Click();
        rendered.WaitForAssertion(() => Assert.False(rendered.Find("#chat2a-sidebar").HasAttribute("hidden")));
        var row = CommonAncestor(rendered.Find("#child"), rendered.Find("#chat2a-sidebar"))!;
        var rowClasses = row.ClassList.ToArray();

        var cssPath = Path.Combine(AppContext.BaseDirectory, "site.css");
        Assert.True(File.Exists(cssPath), $"Expected copied site.css at {cssPath}");
        var css = File.ReadAllText(cssPath);

        // No sidebar rule makes it a fixed/absolute overlay.
        var sidebarDeclarations = Regex.Matches(css, @"([^{}]*(?:chat2a-sidebar|chat-sidebar)[^{}]*)\{([^{}]*)\}")
            .Where(match => !match.Groups[1].Value.Contains("chat-sidebar-", StringComparison.Ordinal)
                || Regex.IsMatch(match.Groups[1].Value, @"(?:\.chat-sidebar|#chat2a-sidebar)(?![-\w])"))
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
            || Regex.IsMatch(body, @"(?:chat2a-sidebar|\.chat-sidebar)(?![-\w])[^{}]*\{[^{}]*(?:width|height|flex)"));
    }

    // AC2 (leaving / hides the panel)
    [Fact]
    public void AC2_LeavingTheMapPage_HidesTheOpenPanel()
    {
        using var context = CreateContext(new ScriptedChatHandler());
        var rendered = RenderLayout(context);

        OpenChatButton(rendered).Click();
        rendered.WaitForAssertion(() => Assert.False(rendered.Find("#chat2a-sidebar").HasAttribute("hidden")));

        context.Services.GetRequiredService<NavigationManager>().NavigateTo("/hello-world");

        rendered.WaitForAssertion(() =>
        {
            var asides = rendered.FindAll("#chat2a-sidebar");
            Assert.True(asides.Count == 0 || asides[0].HasAttribute("hidden"), "Sidebar must be hidden off the map page.");
            Assert.Empty(rendered.FindAll("button[aria-label=\"Open chat\"]"));
        });
    }

    // AC3
    [Fact]
    public void AC3_SendPostsSessionIdAndMessageToChat2a_AndRendersStreamedEvents()
    {
        var handler = new ScriptedChatHandler(
            ScriptedChatHandler.Sse(
                new { type = "session", sessionId = "sidebar-session-1" },
                new { type = "tool_start", toolName = "AddUserCity", toolArguments = "{\"location\":\"Nashville\"}" },
                new { type = "tool_end", toolName = "AddUserCity", toolResult = "{\"ok\":true}" },
                new { type = "token", text = "Added " },
                new { type = "token", text = "Nashville to your cities." },
                new { type = "done" }),
            ScriptedChatHandler.Sse(
                new { type = "error", errorMessage = "Chat2a failed upstream." },
                new { type = "done" }));
        using var context = CreateContext(handler);
        var composer = new ComposerValue(context);
        var rendered = RenderSidebar(context);

        Send(rendered, composer, "add Nashville to my cities");

        rendered.WaitForAssertion(() =>
        {
            Assert.Contains("Added Nashville to your cities.", rendered.Markup);
            Assert.Contains("AddUserCity", rendered.Markup);
        });

        Assert.Single(handler.Requests);
        var first = handler.Requests[0];
        Assert.Equal(HttpMethod.Post, first.Method);
        Assert.EndsWith("/Chat2a/messages", first.Path);
        AssertBody(first.Body, expectedSessionId: null, expectedMessage: "add Nashville to my cities");

        Send(rendered, composer, "what else?");

        rendered.WaitForAssertion(() => Assert.Contains("Chat2a failed upstream.", rendered.Markup));

        Assert.Equal(2, handler.Requests.Count);
        Assert.EndsWith("/Chat2a/messages", handler.Requests[1].Path);
        AssertBody(handler.Requests[1].Body, expectedSessionId: "sidebar-session-1", expectedMessage: "what else?");
    }

    // AC4
    [Fact]
    public void AC4_EveryCompletedSend_IncludingAFailedOne_InvokesWeatherMapRefreshCitiesOnce()
    {
        var handler = new ScriptedChatHandler(
            ScriptedChatHandler.Sse(new { type = "token", text = "first reply" }, new { type = "done" }),
            ScriptedChatHandler.Failure(HttpStatusCode.InternalServerError),
            ScriptedChatHandler.Sse(new { type = "token", text = "third reply" }, new { type = "done" }));
        using var context = CreateContext(handler);
        var composer = new ComposerValue(context);
        var rendered = RenderSidebar(context);

        Assert.Empty(context.JSInterop.Invocations["weatherMap.refreshCities"]);

        Send(rendered, composer, "one");
        rendered.WaitForAssertion(() => Assert.Contains("first reply", rendered.Markup));
        rendered.WaitForAssertion(() => context.JSInterop.VerifyInvoke("weatherMap.refreshCities", 1));

        Send(rendered, composer, "two");
        rendered.WaitForAssertion(() => context.JSInterop.VerifyInvoke("weatherMap.refreshCities", 2));

        Send(rendered, composer, "three");
        rendered.WaitForAssertion(() => Assert.Contains("third reply", rendered.Markup));
        rendered.WaitForAssertion(() => context.JSInterop.VerifyInvoke("weatherMap.refreshCities", 3));

        Assert.Equal(3, handler.Requests.Count);

        // Exactly one refresh per send, all through weatherMap.refreshCities; the sidebar makes
        // no /User request of its own.
        Assert.All(handler.Requests, request => Assert.EndsWith("/Chat2a/messages", request.Path));
        Assert.Equal(3, context.JSInterop.Invocations["weatherMap.refreshCities"].Count);
    }

    // AC4 (edge: the request throws)
    [Fact]
    public void AC4_ASendWhoseRequestThrows_StillInvokesRefreshCitiesExactlyOnce()
    {
        var handler = new ScriptedChatHandler(ScriptedChatHandler.Throws());
        using var context = CreateContext(handler);
        var composer = new ComposerValue(context);
        var rendered = RenderSidebar(context);

        Send(rendered, composer, "one");

        rendered.WaitForAssertion(() => context.JSInterop.VerifyInvoke("weatherMap.refreshCities", 1));
        rendered.WaitForAssertion(() => Assert.Empty(rendered.FindAll("#chat2a-sidebar textarea[disabled]")));
        Assert.Single(context.JSInterop.Invocations["weatherMap.refreshCities"]);
    }

    // AC4 (edge: no JS runtime / no map mounted must not break the sidebar)
    [Fact]
    public void AC4_RefreshCitiesJsFailure_DoesNotBreakTheSidebar()
    {
        var handler = new ScriptedChatHandler(
            ScriptedChatHandler.Sse(new { type = "token", text = "first reply" }, new { type = "done" }),
            ScriptedChatHandler.Sse(new { type = "token", text = "second reply" }, new { type = "done" }));
        using var context = CreateContext(handler);
        context.JSInterop.SetupVoid("weatherMap.refreshCities").SetException(new Microsoft.JSInterop.JSException("weatherMap is not defined"));
        var composer = new ComposerValue(context);
        var rendered = RenderSidebar(context);

        Send(rendered, composer, "one");
        rendered.WaitForAssertion(() => Assert.Contains("first reply", rendered.Markup));

        Send(rendered, composer, "two");
        rendered.WaitForAssertion(() => Assert.Contains("second reply", rendered.Markup));

        Assert.Equal(2, handler.Requests.Count);
        rendered.WaitForAssertion(() => context.JSInterop.VerifyInvoke("weatherMap.refreshCities", 2));
    }

    // AC5
    [Fact]
    public void AC5_WeatherMapJs_ExportsRefreshCities_WhichRereadsUserAndRepaintsPins()
    {
        var script = ReadRepoFile("ui-blazor/blazor/wwwroot/js/weatherMap.js");

        // Exported on the returned module object so the sidebar can call weatherMap.refreshCities().
        var exports = Regex.Match(script, @"return\s*\{[^{}]*\}\s*;\s*\}\)\(\);\s*$", RegexOptions.Singleline);
        Assert.True(exports.Success, "Expected the weatherMap IIFE to end with a returned export object.");
        Assert.Matches(@"refreshCities\s*:\s*refreshCities", exports.Value);

        // refreshCities re-reads /User and repaints from that response.
        var refresh = FunctionBody(script, "refreshCities");
        Assert.Contains("'/User'", refresh);
        Assert.Contains("renderCities(", refresh);

        // renderCities replaces the pin set: old markers removed, new ones created from the response.
        var render = FunctionBody(script, "renderCities");
        Assert.Contains("setMap(null)", render);
        Assert.Contains("createCityMarkers(", render);
    }

    // AC6 (markdown)
    [Fact]
    public void AC6_FinishedReply_RendersSanitizedGfmMarkdown()
    {
        const string reply = "**Warmest**\n\n| City | Temp |\n| --- | --- |\n| Nashville | 72 |\n\n<script>alert(1)</script>";
        var handler = new ScriptedChatHandler(
            ScriptedChatHandler.Sse(new { type = "token", text = reply }, new { type = "done" }));
        using var context = CreateContext(handler);
        var composer = new ComposerValue(context);
        var rendered = RenderSidebar(context);

        Send(rendered, composer, "compare");

        rendered.WaitForAssertion(() =>
        {
            var markdown = rendered.Find("#chat2a-sidebar .chat-markdown");
            Assert.NotNull(markdown.QuerySelector("strong"));
            Assert.NotNull(markdown.QuerySelector("table"));
        });
        Assert.Empty(rendered.FindAll("#chat2a-sidebar script"));
        Assert.DoesNotContain("**Warmest**", rendered.Find("#chat2a-sidebar .chat-markdown").TextContent);
        Assert.Contains(
            Normalize(SafeGfmMarkdown.ToHtml(reply)),
            Normalize(rendered.Find("#chat2a-sidebar .chat-markdown").InnerHtml));
    }

    // AC6 (usage chip)
    [Fact]
    public void AC6_FinishedReplyWithUsage_ShowsUsageChip_WithHoverDetails()
    {
        var usage = new ChatUsage
        {
            RuntimeMs = 1240,
            InputTokenCount = 3100,
            CachedTokenCount = 200,
            OutputTokenCount = 1118,
            ReasoningTokenCount = 40,
            TotalTokenCount = 4218,
        };
        var handler = new ScriptedChatHandler(
            ScriptedChatHandler.Sse(
                new { type = "token", text = "Nashville looks clear." },
                new
                {
                    type = "done",
                    usage = new
                    {
                        runtimeMs = 1240,
                        inputTokenCount = 3100,
                        cachedTokenCount = 200,
                        outputTokenCount = 1118,
                        reasoningTokenCount = 40,
                        totalTokenCount = 4218,
                    },
                }));
        using var context = CreateContext(handler);
        var composer = new ComposerValue(context);
        var rendered = RenderSidebar(context);

        Send(rendered, composer, "weather");

        rendered.WaitForAssertion(() =>
        {
            var chip = rendered.Find("#chat2a-sidebar .chat-markdown .chat-usage-chip[data-tool-details]");
            Assert.Equal(WeatherGridFormat.FormatChatUsageChip(usage), chip.TextContent.Trim());
            Assert.Equal("1.24s · 4,218 tok", chip.TextContent.Trim());
            Assert.Equal(WeatherGridFormat.FormatChatUsageDetails(usage), chip.GetAttribute("data-tool-details"));
            Assert.Equal("0", chip.GetAttribute("tabindex"));
        });
    }

    // AC6 (usage chip: negative)
    [Fact]
    public void AC6_FinishedReplyWithoutUsage_ShowsNoUsageChip()
    {
        var handler = new ScriptedChatHandler(
            ScriptedChatHandler.Sse(new { type = "token", text = "No usage here." }, new { type = "done" }));
        using var context = CreateContext(handler);
        var composer = new ComposerValue(context);
        var rendered = RenderSidebar(context);

        Send(rendered, composer, "hi");

        rendered.WaitForAssertion(() => Assert.Contains("No usage here.", rendered.Find("#chat2a-sidebar .chat-markdown").TextContent));
        Assert.Empty(rendered.FindAll("#chat2a-sidebar .chat-usage-chip"));
    }

    // AC6 (tool hover card)
    [Fact]
    public void AC6_ToolLines_CarryWaitingThenArgumentsAndResultHoverDetails()
    {
        var stream = new PushSse();
        var handler = new ScriptedChatHandler(stream.Reply);
        using var context = CreateContext(handler);
        var composer = new ComposerValue(context);
        var rendered = RenderSidebar(context);

        Send(rendered, composer, "add Nashville");

        stream.Push(new { type = "tool_start", toolName = "AddUserCity" });
        rendered.WaitForAssertion(() =>
        {
            var tool = rendered.Find("#chat2a-sidebar .chat-message.tool[data-tool-details]");
            Assert.Contains("AddUserCity", tool.TextContent);
            Assert.Equal("Waiting for tool output…", tool.GetAttribute("data-tool-details"));
            Assert.Equal("0", tool.GetAttribute("tabindex"));
        });

        stream.Push(new
        {
            type = "tool_end",
            toolName = "AddUserCity",
            toolArguments = "{\"location\":\"Nashville, TN\"}",
            toolResult = "{\"ok\":true}",
        });
        stream.Push(new { type = "token", text = "Added." });
        stream.Push(new { type = "done" });
        stream.Complete();

        rendered.WaitForAssertion(() =>
        {
            var tool = rendered.Find("#chat2a-sidebar .chat-message.tool[data-tool-details]");
            Assert.Equal(
                "Arguments\n{\"location\":\"Nashville, TN\"}\n\nResult\n{\"ok\":true}",
                tool.GetAttribute("data-tool-details"));
        });
    }

    // AC6 (same shared rendering as the /chat-clients panel)
    [Fact]
    public void AC6_SidebarMessages_RenderIdenticallyToChatPanelMessages()
    {
        Func<HttpResponseMessage> Script() => ScriptedChatHandler.Sse(
            new { type = "tool_start", toolName = "AddUserCity", toolArguments = "{\"location\":\"Nashville\"}" },
            new { type = "tool_end", toolName = "AddUserCity", toolArguments = "{\"location\":\"Nashville\"}", toolResult = "{\"ok\":true}" },
            new { type = "token", text = "**Added** Nashville." },
            new { type = "done", usage = new { runtimeMs = 842, totalTokenCount = 345 } });

        using var sidebarContext = CreateContext(new ScriptedChatHandler(Script()));
        var sidebarComposer = new ComposerValue(sidebarContext);
        var sidebar = RenderSidebar(sidebarContext);
        Send(sidebar, sidebarComposer, "add Nashville");
        sidebar.WaitForAssertion(() => Assert.NotEmpty(sidebar.FindAll("#chat2a-sidebar .chat-usage-chip")));

        using var panelContext = CreateContext(new ScriptedChatHandler(Script()));
        new ComposerValue(panelContext).Value = "add Nashville";
        var panel = panelContext.Render<ChatPanel>();
        panel.Find("form.chat-form").Submit();
        panel.WaitForAssertion(() => Assert.NotEmpty(panel.FindAll(".chat-usage-chip")));

        Assert.Equal(
            MessageSignature(panel.FindAll(".chat-message")),
            MessageSignature(sidebar.FindAll("#chat2a-sidebar .chat-message")));
    }

    // ---- helpers ----

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

    private static IRenderedComponent<Chat2aSidebar> RenderSidebar(BunitContext context)
        => context.Render<Chat2aSidebar>(parameters => parameters
            .Add(sidebar => sidebar.Open, true)
            .Add(sidebar => sidebar.OnClose, () => { }));

    private static AngleSharp.Dom.IElement OpenChatButton(IRenderedComponent<MainLayout> rendered)
        => rendered.Find(".header-actions button[aria-label=\"Open chat\"]");

    private static string[] HeaderButtonLabels(IRenderedComponent<MainLayout> rendered)
        => rendered.FindAll(".header-actions button[aria-label]")
            .Select(button => button.GetAttribute("aria-label") ?? string.Empty)
            .ToArray();

    /// <summary>
    /// Types into the sidebar composer and submits. Works whether the composer is
    /// uncontrolled (read via chatInput.getValue, like ChatPanel) or @bind-ed.
    /// </summary>
    private static void Send(IRenderedComponent<Chat2aSidebar> rendered, ComposerValue composer, string message)
    {
        composer.Value = message;
        var textarea = rendered.Find("#chat2a-sidebar textarea");
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

        rendered.Find("#chat2a-sidebar form").Submit();
    }

    private static void PressEscape(IRenderedComponent<MainLayout> rendered)
    {
        try
        {
            rendered.Find("#chat2a-sidebar").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Escape" });
        }
        catch (MissingEventHandlerException)
        {
            rendered.Find("#chat2a-sidebar textarea").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Escape" });
        }
    }

    private static AngleSharp.Dom.IElement? CommonAncestor(AngleSharp.Dom.IElement a, AngleSharp.Dom.IElement b)
    {
        var node = a.ParentElement;
        while (node is not null && !node.Contains(b))
        {
            node = node.ParentElement;
        }

        return node;
    }

    private static string Normalize(string html) => Regex.Replace(html, @">\s+<", "><").Trim();

    private static string[] MessageSignature(IEnumerable<AngleSharp.Dom.IElement> messages)
        => messages
            .Select(message => string.Join(
                " | ",
                string.Join(' ', message.ClassList.OrderBy(name => name)),
                Normalize(message.InnerHtml),
                message.GetAttribute("data-tool-details") ?? string.Empty))
            .ToArray();

    private static void AssertBody(string body, string? expectedSessionId, string expectedMessage)
    {
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        var names = root.EnumerateObject().Select(property => property.Name).OrderBy(name => name).ToArray();
        Assert.Equal(new[] { "message", "sessionId" }, names);
        Assert.Equal(expectedMessage, root.GetProperty("message").GetString());
        if (expectedSessionId is null)
        {
            Assert.Equal(JsonValueKind.Null, root.GetProperty("sessionId").ValueKind);
        }
        else
        {
            Assert.Equal(expectedSessionId, root.GetProperty("sessionId").GetString());
        }
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

    private static string FunctionBody(string script, string name)
    {
        var start = script.IndexOf($"function {name}(", StringComparison.Ordinal);
        Assert.True(start >= 0, $"Expected function {name} in weatherMap.js");
        var open = script.IndexOf('{', start);
        var depth = 0;
        for (var i = open; i < script.Length; i++)
        {
            if (script[i] == '{') depth++;
            else if (script[i] == '}' && --depth == 0) return script[open..(i + 1)];
        }

        return script[open..];
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
            var reply = _replies.Count > 0
                ? _replies.Dequeue()
                : Sse(new { type = "done" });
            return reply();
        }
    }

    /// <summary>An SSE response whose events the test pushes one at a time.</summary>
    private sealed class PushSse
    {
        private readonly Channel<byte[]> _chunks = Channel.CreateUnbounded<byte[]>();

        public Func<HttpResponseMessage> Reply => () => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(new ChannelStream(_chunks.Reader)),
        };

        public void Push(object streamEvent)
            => _chunks.Writer.TryWrite(Encoding.UTF8.GetBytes($"data: {JsonSerializer.Serialize(streamEvent)}\n\n"));

        public void Complete() => _chunks.Writer.TryComplete();

        private sealed class ChannelStream(ChannelReader<byte[]> reader) : Stream
        {
            private byte[] _current = [];
            private int _offset;

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

            public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                while (_offset >= _current.Length)
                {
                    if (!await reader.WaitToReadAsync(cancellationToken))
                    {
                        return 0;
                    }

                    if (reader.TryRead(out var next))
                    {
                        _current = next;
                        _offset = 0;
                    }
                }

                var count = Math.Min(buffer.Length, _current.Length - _offset);
                _current.AsMemory(_offset, count).CopyTo(buffer);
                _offset += count;
                return count;
            }

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
                => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

            public override int Read(byte[] buffer, int offset, int count)
                => ReadAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();

            public override void Flush() { }
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }
    }
}
