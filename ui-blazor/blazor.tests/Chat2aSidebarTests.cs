using System.Net;
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

public sealed class Chat2aSidebarTests
{
    [Fact]
    public void MainLayout_PlacesOpenChatBetweenAddLocationAndAvatar()
    {
        using var context = CreateLayoutContext();

        var rendered = context.Render<MainLayout>(parameters => parameters.Add(layout => layout.Body, "<div id=\"child\"></div>"));

        AssertBefore(rendered.Markup, "aria-label=\"Add location\"", "aria-label=\"Open chat\"");
        AssertBefore(rendered.Markup, "aria-label=\"Open chat\"", "id=\"user-menu-button\"");

        context.Services.GetRequiredService<NavigationManager>().NavigateTo("/hello-world");

        rendered.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("aria-label=\"Add location\"", rendered.Markup);
            AssertBefore(rendered.Markup, "aria-label=\"Open chat\"", "id=\"user-menu-button\"");
        });
    }

    [Fact]
    public void MainLayout_ChatButtonTogglesTheSidebar_AndCloseAndEscapeHideIt()
    {
        using var context = CreateLayoutContext();
        var rendered = context.Render<MainLayout>(parameters => parameters.Add(layout => layout.Body, "<div id=\"child\"></div>"));

        var toggle = rendered.Find("button[aria-label=\"Open chat\"]");
        Assert.Equal("false", toggle.GetAttribute("aria-expanded"));
        Assert.Equal("chat2a-sidebar", toggle.GetAttribute("aria-controls"));
        var sidebar = rendered.Find("#chat2a-sidebar");
        Assert.Equal("complementary", sidebar.GetAttribute("role"));
        Assert.Equal("Chat2a", sidebar.GetAttribute("aria-label"));
        Assert.True(sidebar.HasAttribute("hidden"));

        toggle.Click();
        Assert.Equal("true", rendered.Find("button[aria-label=\"Open chat\"]").GetAttribute("aria-expanded"));
        Assert.False(rendered.Find("#chat2a-sidebar").HasAttribute("hidden"));

        rendered.Find("button[aria-label=\"Open chat\"]").Click();
        Assert.Equal("false", rendered.Find("button[aria-label=\"Open chat\"]").GetAttribute("aria-expanded"));
        Assert.True(rendered.Find("#chat2a-sidebar").HasAttribute("hidden"));

        rendered.Find("button[aria-label=\"Open chat\"]").Click();
        rendered.Find("button[aria-label=\"Close chat\"]").Click();
        Assert.Equal("false", rendered.Find("button[aria-label=\"Open chat\"]").GetAttribute("aria-expanded"));
        Assert.True(rendered.Find("#chat2a-sidebar").HasAttribute("hidden"));

        rendered.Find("button[aria-label=\"Open chat\"]").Click();
        rendered.Find("#chat2a-sidebar").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Escape" });
        Assert.Equal("false", rendered.Find("button[aria-label=\"Open chat\"]").GetAttribute("aria-expanded"));
        Assert.True(rendered.Find("#chat2a-sidebar").HasAttribute("hidden"));
    }

    [Fact]
    public void Send_PostsChat2aWithSession_RendersEvents_AndRefreshesCitiesEachCompletion()
    {
        var handler = new StubChatHandler(
            Sse(
                new { type = "session", sessionId = "s-1" },
                new { type = "tool_start", toolName = "AddUserCity" },
                new { type = "tool_end", toolName = "AddUserCity", toolResult = "ok" },
                new { type = "token", text = "Added " },
                new { type = "token", text = "Nashville." },
                new { type = "done" }),
            Sse(
                new { type = "session", sessionId = "s-1" },
                new { type = "error", errorMessage = "Agent failed." },
                new { type = "done" }));
        using var context = CreateSidebarContext(handler);
        var rendered = context.Render<Chat2aSidebar>(parameters => parameters.Add(sidebar => sidebar.Open, true));

        SendMessage(rendered, "add Nashville");
        rendered.WaitForAssertion(() =>
        {
            Assert.Contains("Added Nashville.", rendered.Markup);
            Assert.Contains("Ran AddUserCity …", rendered.Markup);
            Assert.Equal(1, context.JSInterop.Invocations.Count(i => i.Identifier == "weatherMap.refreshCities"));
        });

        SendMessage(rendered, "remove Nashville");
        rendered.WaitForAssertion(() =>
        {
            Assert.Contains("Agent failed.", rendered.Markup);
            Assert.Equal(2, context.JSInterop.Invocations.Count(i => i.Identifier == "weatherMap.refreshCities"));
        });

        Assert.Equal(2, handler.Requests.Count);
        Assert.All(handler.Requests, request =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/Chat2a/messages", request.Path);
        });

        using var first = JsonDocument.Parse(handler.Requests[0].Body);
        Assert.Equal(JsonValueKind.Null, first.RootElement.GetProperty("sessionId").ValueKind);
        Assert.Equal("add Nashville", first.RootElement.GetProperty("message").GetString());
        Assert.Equal(2, first.RootElement.EnumerateObject().Count());

        using var second = JsonDocument.Parse(handler.Requests[1].Body);
        Assert.Equal("s-1", second.RootElement.GetProperty("sessionId").GetString());
        Assert.Equal("remove Nashville", second.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public void Send_FailedRequest_ShowsErrorAndStillRefreshesCities()
    {
        var handler = new StubChatHandler(statusCode: HttpStatusCode.InternalServerError);
        using var context = CreateSidebarContext(handler);
        var rendered = context.Render<Chat2aSidebar>(parameters => parameters.Add(sidebar => sidebar.Open, true));

        SendMessage(rendered, "hello");

        rendered.WaitForAssertion(() =>
        {
            Assert.Contains("chat-message error", rendered.Markup);
            Assert.Equal(1, context.JSInterop.Invocations.Count(i => i.Identifier == "weatherMap.refreshCities"));
        });
    }

    [Fact]
    public void Sidebar_UsesIdsDistinctFromTheChatClientsPage()
    {
        using var context = CreateSidebarContext(new StubChatHandler());
        var rendered = context.Render<Chat2aSidebar>();

        Assert.NotNull(rendered.Find("#chat2a-sidebar-input"));
        Assert.DoesNotContain("id=\"chat-input\"", rendered.Markup);
        Assert.DoesNotContain("id=\"chat-messages\"", rendered.Markup);
    }

    [Fact]
    public void WeatherMapJs_ExportsRefreshCities()
    {
        var source = File.ReadAllText(RepoFiles.FindRepoFile("ui-blazor/blazor/wwwroot/js/weatherMap.js"));

        Assert.Contains("refreshCities: refreshCities", source);
    }

    [Fact]
    public void SiteCss_AnchorsSidebarRightBelowHeader_AndFullWidthOnPhones()
    {
        var css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "site.css"));

        Assert.Contains(".chat-sidebar {", css);
        Assert.Contains("top: 48px;", css);
        Assert.Contains("@media (max-width: 639.98px)", css);
        Assert.Contains(".chat-toggle-button", css);
    }

    private static void SendMessage(IRenderedComponent<Chat2aSidebar> rendered, string message)
    {
        rendered.WaitForAssertion(() => Assert.False(rendered.Find("#chat2a-sidebar-input").HasAttribute("disabled")));
        rendered.Find("#chat2a-sidebar-input").Change(message);
        rendered.Find("form.chat-sidebar-form").Submit();
    }

    private static void AssertBefore(string markup, string first, string second)
    {
        var firstIndex = markup.IndexOf(first, StringComparison.Ordinal);
        var secondIndex = markup.IndexOf(second, StringComparison.Ordinal);
        Assert.True(firstIndex >= 0, $"Missing {first}");
        Assert.True(secondIndex >= 0, $"Missing {second}");
        Assert.True(firstIndex < secondIndex, $"Expected {first} before {second}");
    }

    private static BunitContext CreateLayoutContext()
    {
        var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.Services.AddHttpClient();
        context.Services.AddFluentUIComponents();
        context.Services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        context.Services.AddSingleton(
            new WeatherApiClient(new HttpClient { BaseAddress = new Uri("http://localhost/") }, NullLogger<WeatherApiClient>.Instance));
        context.Services.AddSingleton(new ChatApiClient(new HttpClient(new StubChatHandler()) { BaseAddress = new Uri("http://localhost/") }));
        return context;
    }

    private static BunitContext CreateSidebarContext(StubChatHandler handler)
    {
        var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.Services.AddSingleton(new ChatApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }));
        return context;
    }

    private static string Sse(params object[] events)
    {
        var builder = new StringBuilder();
        foreach (var streamEvent in events)
        {
            builder.Append("data: ").Append(JsonSerializer.Serialize(streamEvent)).Append("\n\n");
        }

        return builder.ToString();
    }

    private sealed record CapturedRequest(HttpMethod Method, string Path, string Body);

    private sealed class StubChatHandler(params string[] responses) : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode = HttpStatusCode.OK;

        public StubChatHandler(HttpStatusCode statusCode)
            : this()
        {
            _statusCode = statusCode;
        }

        public List<CapturedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add(new CapturedRequest(request.Method, request.RequestUri!.AbsolutePath, body));

            var index = Requests.Count - 1;
            var payload = index < responses.Length ? responses[index] : string.Empty;
            return new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(payload, Encoding.UTF8, "text/event-stream"),
            };
        }
    }
}
