using System.Net;
using System.Net.Http.Json;
using System.Text;
using Bunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.FluentUI.AspNetCore.Components;
using WeatherBlazor.Data;
using WeatherBlazor.Shared;

namespace WeatherBlazor.Tests;

public sealed class PageSplitTests
{
    [Fact]
    public void Index_RendersMapWithoutSplitPageContent()
    {
        using var context = CreateContext();
        var rendered = context.Render<WeatherBlazor.Pages.Index>();

        Assert.Contains("id=\"weather-map\"", rendered.Markup);
        Assert.DoesNotContain("Chat Clients", rendered.Markup);
        Assert.DoesNotContain("Current AI Weather", rendered.Markup);
        Assert.DoesNotContain("Hello World", rendered.Markup);
        Assert.DoesNotContain("Loading hello message", rendered.Markup);
    }

    [Fact]
    public void HelloWorld_RendersHelloWithoutWeatherChatOrMap()
    {
        using var context = CreateContext();
        var rendered = context.Render<WeatherBlazor.Pages.HelloWorld>();

        rendered.WaitForAssertion(() =>
        {
            Assert.Contains("Hello from test API.", rendered.Markup);
        });

        Assert.Contains("Hello World", rendered.Markup);
        Assert.DoesNotContain("Chat Clients", rendered.Markup);
        Assert.DoesNotContain("Current AI Weather", rendered.Markup);
        Assert.DoesNotContain("chat-input", rendered.Markup);
        Assert.DoesNotContain("id=\"weather-map\"", rendered.Markup);
    }

    [Fact]
    public void CurrentAIWeather_RendersWeatherFormWithoutHelloChatOrMap()
    {
        using var context = CreateContext();
        var rendered = context.Render<WeatherBlazor.Pages.CurrentAIWeather>();

        Assert.Contains("Current AI Weather", rendered.Markup);
        Assert.Contains("Get Current AI Weather", rendered.Markup);
        Assert.DoesNotContain("Hello World", rendered.Markup);
        Assert.DoesNotContain("Chat Clients", rendered.Markup);
        Assert.DoesNotContain("chat-input", rendered.Markup);
        Assert.DoesNotContain("id=\"weather-map\"", rendered.Markup);
    }

    [Fact]
    public void ChatClients_RendersChatWithoutHelloWeatherOrMap()
    {
        using var context = CreateContext();
        var rendered = context.Render<WeatherBlazor.Pages.ChatClients>();

        Assert.Contains("Chat Clients", rendered.Markup);
        Assert.Contains("chat-input", rendered.Markup);
        Assert.DoesNotContain("Hello World", rendered.Markup);
        Assert.DoesNotContain("Current AI Weather", rendered.Markup);
        Assert.DoesNotContain("Loading hello message", rendered.Markup);
        Assert.DoesNotContain("id=\"weather-map\"", rendered.Markup);
    }

    private static BunitContext CreateContext()
    {
        var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.Services.AddHttpClient();
        context.Services.AddFluentUIComponents();
        context.Services.AddSingleton<IConfiguration>(
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["GOOGLE_MAPS_API_KEY"] = "",
                    ["API_DOTNET_URL"] = "http://localhost:8080",
                })
                .Build());

        var http = new HttpClient(new StubHelloHandler())
        {
            BaseAddress = new Uri("http://localhost/"),
        };
        context.Services.AddSingleton(new WeatherApiClient(http, NullLogger<WeatherApiClient>.Instance));
        context.Services.AddSingleton(new ChatApiClient(http));
        return context;
    }

    private sealed class StubHelloHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            if (path.EndsWith("/Home/Hello", StringComparison.OrdinalIgnoreCase)
                || path.Equals("/Home/Hello", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith("Home/Hello", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new HelloWorldResponse
                    {
                        RequestMessage = "from test",
                        RequestResponse = "Hello from test API.",
                    }),
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json"),
            });
        }
    }
}
