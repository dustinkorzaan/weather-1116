using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.FluentUI.AspNetCore.Components;
using WeatherBlazor.Data;
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

    // AC1 (edge: route without the plus button)
    [Fact]
    public void AC1_OnRoutesWithoutAddLocation_OpenChatSitsImmediatelyBeforeAvatar()
    {
        using var context = CreateContext(new ScriptedChatHandler());
        var rendered = RenderLayout(context);

        context.Services.GetRequiredService<NavigationManager>().NavigateTo("/hello-world");

        rendered.WaitForAssertion(() =>
            Assert.Equal(new[] { "Open chat", "Open user menu" }, HeaderButtonLabels(rendered)));
    }

    // AC2
    [Fact]
    public void AC2_OpenChatTogglesChat2aComplementaryPanel_AndAriaExpanded()
    {
        using var context = CreateContext(new ScriptedChatHandler());
        var rendered = RenderLayout(context);

        var aside = rendered.Find("#chat2a-sidebar");
        Assert.Equal("Chat2a", aside.GetAttribute("aria-label"));
        Assert.Contains(aside.GetAttribute("role"), new[] { "complementary", "dialog" });
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

    // AC2 (right edge below the 48px header; full width under 640px)
    [Fact]
    public void AC2_SiteCss_AnchorsSidebarRight_BelowHeader_FullWidthUnder640()
    {
        var cssPath = Path.Combine(AppContext.BaseDirectory, "site.css");
        Assert.True(File.Exists(cssPath), $"Expected copied site.css at {cssPath}");
        var css = File.ReadAllText(cssPath);

        var sidebarRules = Regex.Matches(css, @"([^{}]*(?:chat2a-sidebar|chat-sidebar)[^{}]*)\{([^{}]*)\}")
            .Select(match => match.Groups[2].Value)
            .ToList();
        Assert.NotEmpty(sidebarRules);

        var declarations = string.Join("\n", sidebarRules);
        Assert.Matches(@"position:\s*fixed", declarations);
        Assert.Matches(@"right:\s*0", declarations);
        Assert.Matches(@"top:\s*48px", declarations);

        var narrowBlocks = Regex.Matches(css, @"@media[^{]*max-width:\s*6[0-3]\d(?:\.\d+)?px[^{]*\{((?:[^{}]*\{[^{}]*\})*)[^{}]*\}")
            .Select(match => match.Groups[1].Value)
            .Where(body => Regex.IsMatch(body, @"(?:chat2a-sidebar|chat-sidebar)[^{}]*\{[^{}]*width:\s*100%"))
            .ToList();
        Assert.True(narrowBlocks.Count > 0, "Expected a <640px media rule giving the sidebar width: 100%.");
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
        var script = File.ReadAllText(RepoFiles.FindRepoFile("ui-blazor/blazor/wwwroot/js/weatherMap.js"));

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
}
