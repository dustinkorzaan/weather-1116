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

// Implementation details the acceptance tests (Chat2aSidebarAcceptanceTests) do not cover.
public sealed class Chat2aSidebarTests
{
    [Fact]
    public void MainLayout_DocksSidebarInTheBodyRowOnMapPage_AndKeepsItMountedWithHistoryOffIt()
    {
        var handler = new StubChatHandler(Sse(
            new { type = "session", sessionId = "s-1" },
            new { type = "token", text = "Hi there." },
            new { type = "done" }));
        using var context = CreateLayoutContext(handler);
        var rendered = context.Render<MainLayout>(parameters => parameters.Add(layout => layout.Body, "<div id=\"child\"></div>"));

        var row = rendered.Find(".fluent-layout-item.weather-body > .weather-main");
        Assert.Contains("is-map-page", row.ClassName);
        Assert.NotNull(rendered.Find(".weather-main > .weather-main-page > #child"));
        Assert.NotNull(rendered.Find(".weather-main > #chat2a-sidebar"));

        rendered.Find("button[aria-label=\"Open chat\"]").Click();
        SendLayoutMessage(rendered, "hello");
        rendered.WaitForAssertion(() => Assert.Contains("Hi there.", rendered.Find("#chat2a-sidebar").InnerHtml));

        var navigation = context.Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/hello-world");
        rendered.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("is-map-page", rendered.Find(".weather-main").ClassName);
            Assert.True(rendered.Find("#chat2a-sidebar").HasAttribute("hidden"));
            Assert.Contains("Hi there.", rendered.Find("#chat2a-sidebar").InnerHtml);
        });

        navigation.NavigateTo("/");
        rendered.WaitForAssertion(() =>
        {
            Assert.Contains("is-map-page", rendered.Find(".weather-main").ClassName);
            Assert.True(rendered.Find("#chat2a-sidebar").HasAttribute("hidden"));
            Assert.Equal("false", rendered.Find("button[aria-label=\"Open chat\"]").GetAttribute("aria-expanded"));
        });

        rendered.Find("button[aria-label=\"Open chat\"]").Click();
        Assert.Contains("Hi there.", rendered.Find("#chat2a-sidebar").InnerHtml);
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
    public void ChatPanelAndSidebar_RenderEntriesThroughTheSharedMessageList()
    {
        foreach (var file in new[] { "ChatPanel.razor", "Chat2aSidebar.razor" })
        {
            var source = File.ReadAllText(RepoFiles.FindRepoFile($"ui-blazor/blazor/Shared/{file}"));

            Assert.Contains("<ChatMessageList Entries=", source);
            Assert.DoesNotContain("SafeGfmMarkdown", source);
            Assert.DoesNotContain("data-tool-details", source);
            Assert.DoesNotContain("class ChatEntry", source);
        }
    }

    [Fact]
    public void Sidebar_FocusesInputWhenOpened_SoEscapeClosesRightAway()
    {
        using var context = CreateSidebarContext(new StubChatHandler());
        var rendered = context.Render<Chat2aSidebar>(parameters => parameters.Add(sidebar => sidebar.Open, false));

        Assert.DoesNotContain(context.JSInterop.Invocations, i => i.Identifier == FocusIdentifier);

        rendered.Render(parameters => parameters.Add(sidebar => sidebar.Open, true));
        Assert.Single(context.JSInterop.Invocations, i => i.Identifier == FocusIdentifier);

        rendered.Render(parameters => parameters.Add(sidebar => sidebar.Open, true));
        Assert.Single(context.JSInterop.Invocations, i => i.Identifier == FocusIdentifier);

        rendered.Render(parameters => parameters.Add(sidebar => sidebar.Open, false));
        rendered.Render(parameters => parameters.Add(sidebar => sidebar.Open, true));
        Assert.Equal(2, context.JSInterop.Invocations.Count(i => i.Identifier == FocusIdentifier));
    }

    [Fact]
    public async Task Dispose_CancelsTheInFlightStream()
    {
        var handler = new HangingChatHandler();
        using var context = CreateSidebarContext(handler);
        var rendered = context.Render<Chat2aSidebar>(parameters => parameters.Add(sidebar => sidebar.Open, true));

        rendered.Find("#chat2a-sidebar-input").Change("hello");
        rendered.Find("form.chat-sidebar-form").Submit();
        await handler.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        await context.DisposeComponentsAsync();

        await handler.Cancelled.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void SiteCss_DocksSidebarBesideTheMap_AndStacksItUnderTheMapOnPhones()
    {
        var css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "site.css"));

        var sidebar = RuleBody(css, ".chat-sidebar {");
        Assert.DoesNotMatch(@"position:\s*fixed", sidebar);
        Assert.Matches(@"flex:\s*0 0 24rem", sidebar);
        Assert.Matches(@"flex-direction:\s*row", RuleBody(css, ".weather-main.is-map-page {"));
        Assert.Matches(@"min-width:\s*0", RuleBody(css, ".weather-main.is-map-page > .weather-main-page {"));
        Assert.Matches(@"display:\s*contents", RuleBody(css, ".weather-main-page {"));

        var phone = css[css.IndexOf("@media (max-width: 639.98px)", StringComparison.Ordinal)..];
        Assert.Matches(@"\.weather-main\.is-map-page \{\s*flex-direction:\s*column", phone);
        Assert.Matches(@"\.chat-sidebar \{[^}]*width:\s*100%", phone);
        Assert.Contains(".chat-toggle-button", css);
    }

    [Fact]
    public void SiteCss_HeaderNoLongerNeedsTheOverlayZIndexWorkaround()
    {
        var css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "site.css"));

        Assert.DoesNotContain("z-index", RuleBody(css, ".fluent-layout.weather-shell > .fluent-layout-item.weather-header {"));
        Assert.DoesNotContain("z-index", RuleBody(css, ".chat-sidebar {"));
    }

    private const string FocusIdentifier = "Blazor._internal.domWrapper.focus";

    private static string RuleBody(string css, string selectorBlock)
    {
        var start = css.IndexOf(selectorBlock, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Missing {selectorBlock}");
        var end = css.IndexOf('}', start);
        return css[start..end];
    }

    private static void SendLayoutMessage(IRenderedComponent<MainLayout> rendered, string message)
    {
        rendered.WaitForAssertion(() => Assert.False(rendered.Find("#chat2a-sidebar-input").HasAttribute("disabled")));
        rendered.Find("#chat2a-sidebar-input").Change(message);
        rendered.Find("form.chat-sidebar-form").Submit();
    }

    private static BunitContext CreateLayoutContext(StubChatHandler handler)
    {
        var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.Services.AddHttpClient();
        context.Services.AddFluentUIComponents();
        context.Services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        context.Services.AddSingleton(
            new WeatherApiClient(new HttpClient { BaseAddress = new Uri("http://localhost/") }, NullLogger<WeatherApiClient>.Instance));
        context.Services.AddSingleton(new ChatApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }));
        return context;
    }

    private static BunitContext CreateSidebarContext(HttpMessageHandler handler)
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

    private sealed class StubChatHandler(params string[] responses) : HttpMessageHandler
    {
        private int _count;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var index = _count++;
            var payload = index < responses.Length ? responses[index] : string.Empty;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "text/event-stream"),
            });
        }
    }

    // Never answers: completes Cancelled when the sidebar's token fires.
    private sealed class HangingChatHandler : HttpMessageHandler
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Cancelled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Started.TrySetResult();
            try
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                Cancelled.TrySetResult();
                throw;
            }

            throw new InvalidOperationException("Unreachable.");
        }
    }
}
