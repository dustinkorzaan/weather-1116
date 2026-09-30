using System.Net;
using System.Text;
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
    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<(string Path, string Body)> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add((request.RequestUri!.AbsolutePath, body));
            return respond(request);
        }
    }

    private static HttpResponseMessage Sse(params string[] events)
    {
        var body = string.Concat(events.Select(json => $"data: {json}\n\n"));
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "text/event-stream"),
        };
    }

    private static BunitContext CreateContext(HttpMessageHandler? chatHandler = null)
    {
        var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.Services.AddHttpClient();
        context.Services.AddFluentUIComponents();
        context.Services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        context.Services.AddSingleton(
            new WeatherApiClient(new HttpClient { BaseAddress = new Uri("http://localhost/") }, NullLogger<WeatherApiClient>.Instance));
        var chatHttp = chatHandler is null ? new HttpClient() : new HttpClient(chatHandler);
        chatHttp.BaseAddress = new Uri("http://localhost/");
        context.Services.AddSingleton(new ChatApiClient(chatHttp));
        return context;
    }

    private static IRenderedComponent<MainLayout> RenderLayout(BunitContext context) =>
        context.Render<MainLayout>(parameters => parameters.Add(layout => layout.Body, "<div id=\"child\"></div>"));

    [Fact]
    public void HeaderChatIconSitsBetweenThePlusControlAndTheUserMenu()
    {
        using var context = CreateContext();

        var markup = RenderLayout(context).Markup;

        var addIndex = markup.IndexOf("aria-label=\"Add location\"", StringComparison.Ordinal);
        var chatIndex = markup.IndexOf("aria-label=\"Chat\"", StringComparison.Ordinal);
        var userIndex = markup.IndexOf("id=\"user-menu-button\"", StringComparison.Ordinal);
        Assert.True(addIndex >= 0 && chatIndex > addIndex && userIndex > chatIndex, markup);
    }

    [Fact]
    public void ChatIconIsOnlyOnTheMapPage()
    {
        using var context = CreateContext();
        context.Services.GetRequiredService<NavigationManager>().NavigateTo("/hello-world");

        var markup = RenderLayout(context).Markup;

        Assert.DoesNotContain("aria-label=\"Chat\"", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void ChatIconTogglesTheRightHandSidebar()
    {
        using var context = CreateContext();
        var rendered = RenderLayout(context);

        var sidebar = rendered.Find("#chat2a-sidebar");
        Assert.True(sidebar.HasAttribute("hidden"));
        // The sidebar is the map column's right-hand sibling.
        Assert.Equal("weather-main", sidebar.PreviousElementSibling?.ClassName);

        rendered.Find("button[aria-label=\"Chat\"]").Click();
        Assert.False(rendered.Find("#chat2a-sidebar").HasAttribute("hidden"));
        Assert.Equal("true", rendered.Find("button[aria-label=\"Chat\"]").GetAttribute("aria-expanded"));

        rendered.Find("button[aria-label=\"Close chat\"]").Click();
        Assert.True(rendered.Find("#chat2a-sidebar").HasAttribute("hidden"));
    }

    [Fact]
    public async Task SendingPostsToChat2aAndRefreshesMapCitiesAfterEveryCompletion()
    {
        var handler = new RecordingHandler(_ => Sse(
            "{\"type\":\"session\",\"sessionId\":\"session-1\"}",
            "{\"type\":\"tool_start\",\"toolName\":\"AddUserCity\",\"toolArguments\":\"{}\"}",
            "{\"type\":\"tool_end\",\"toolName\":\"AddUserCity\",\"toolResult\":\"ok\"}",
            "{\"type\":\"token\",\"text\":\"Added Nashville.\"}",
            "{\"type\":\"done\"}"));
        using var context = CreateContext(handler);
        context.JSInterop.Setup<string>("chatInput.getValue", _ => true).SetResult("Add Nashville to my map");

        var rendered = context.Render<Chat2aSidebar>(parameters => parameters.Add(sidebar => sidebar.Open, true));

        await rendered.Find("form").SubmitAsync();
        rendered.WaitForAssertion(() => Assert.Contains("Added Nashville.", rendered.Markup));
        Assert.Contains("Ran AddUserCity", rendered.Markup);
        Assert.Single(context.JSInterop.VerifyInvoke("weatherMap.refreshCities", 1));

        await rendered.Find("form").SubmitAsync();
        rendered.WaitForAssertion(() => Assert.Equal(2, context.JSInterop.Invocations["weatherMap.refreshCities"].Count));

        Assert.Equal(2, handler.Requests.Count);
        Assert.All(handler.Requests, request => Assert.Equal("/Chat2a/messages", request.Path));
        Assert.Contains("\"sessionId\":null", handler.Requests[0].Body);
        Assert.Contains("\"sessionId\":\"session-1\"", handler.Requests[1].Body);
    }

    [Fact]
    public async Task FailedChatStillRefreshesMapCities()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        using var context = CreateContext(handler);
        context.JSInterop.Setup<string>("chatInput.getValue", _ => true).SetResult("hello");

        var rendered = context.Render<Chat2aSidebar>(parameters => parameters.Add(sidebar => sidebar.Open, true));

        await rendered.Find("form").SubmitAsync();
        rendered.WaitForAssertion(() => Assert.Contains("chat-message error", rendered.Markup));
        Assert.Single(context.JSInterop.VerifyInvoke("weatherMap.refreshCities", 1));
    }

    [Fact]
    public void WeatherMapScriptExposesRefreshCities()
    {
        var script = File.ReadAllText(RepoFiles.FindRepoFile("ui-blazor/blazor/wwwroot/js/weatherMap.js"));
        Assert.Contains("refreshCities: refreshCities", script);
    }
}
