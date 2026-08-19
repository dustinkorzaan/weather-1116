using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Core.About;
using Core.Chat.Models;
using Core.Chat.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace WeatherAPI.Tests;

public class Chat1aControllerTests(ChatApiWebApplicationFactory factory) : IClassFixture<ChatApiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task PostMessage_ReturnsCamelCaseSseEvents()
    {
        using var response = await _client.PostAsJsonAsync(
            "/Chat1a/messages",
            new ChatSendMessageRequest { Message = "Hi" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("data: ", body, StringComparison.Ordinal);

        var events = ParseSsePayloads(body);
        Assert.Collection(
            events,
            sessionEvent =>
            {
                Assert.Equal("session", sessionEvent.GetProperty("type").GetString());
                Assert.Equal("Chat1a:test-session", sessionEvent.GetProperty("sessionId").GetString());
            },
            tokenEvent =>
            {
                Assert.Equal("token", tokenEvent.GetProperty("type").GetString());
                Assert.Equal("Hello", tokenEvent.GetProperty("text").GetString());
            },
            doneEvent => Assert.Equal("done", doneEvent.GetProperty("type").GetString()));

        Assert.DoesNotContain("\"Type\"", body, StringComparison.Ordinal);
        Assert.DoesNotContain("\"SessionId\"", body, StringComparison.Ordinal);
    }

    private static List<JsonElement> ParseSsePayloads(string body)
    {
        var events = new List<JsonElement>();

        foreach (var block in body.Split("\n\n", StringSplitOptions.RemoveEmptyEntries))
        {
            var line = block.Trim();
            if (!line.StartsWith("data:", StringComparison.Ordinal))
            {
                continue;
            }

            events.Add(JsonDocument.Parse(line["data:".Length..].Trim()).RootElement.Clone());
        }

        return events;
    }
}

public class Chat3ControllerTests(ChatApiWebApplicationFactory factory) : IClassFixture<ChatApiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task PostMessage_ReturnsCamelCaseSseEvents()
    {
        using var response = await _client.PostAsJsonAsync(
            "/Chat3/messages",
            new ChatSendMessageRequest { Message = "Hi" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("data: ", body, StringComparison.Ordinal);

        var events = ParseSsePayloads(body);
        Assert.Collection(
            events,
            sessionEvent =>
            {
                Assert.Equal("session", sessionEvent.GetProperty("type").GetString());
                Assert.Equal("Chat3:test-session", sessionEvent.GetProperty("sessionId").GetString());
            },
            tokenEvent =>
            {
                Assert.Equal("token", tokenEvent.GetProperty("type").GetString());
                Assert.Equal("Hello from Chat3", tokenEvent.GetProperty("text").GetString());
            },
            doneEvent => Assert.Equal("done", doneEvent.GetProperty("type").GetString()));
    }

    private static List<JsonElement> ParseSsePayloads(string body)
    {
        var events = new List<JsonElement>();

        foreach (var block in body.Split("\n\n", StringSplitOptions.RemoveEmptyEntries))
        {
            var line = block.Trim();
            if (!line.StartsWith("data:", StringComparison.Ordinal))
            {
                continue;
            }

            events.Add(JsonDocument.Parse(line["data:".Length..].Trim()).RootElement.Clone());
        }

        return events;
    }
}

public class Chat3ErrorStreamTests(Chat3ThrowingWebApplicationFactory factory)
    : IClassFixture<Chat3ThrowingWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task PostMessage_WritesErrorEvent_WhenServiceThrowsAfterSession()
    {
        using var response = await _client.PostAsJsonAsync(
            "/Chat3/messages",
            new ChatSendMessageRequest { Message = "Hi" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadAsStringAsync();
        var events = ParseSsePayloads(body);
        Assert.Collection(
            events,
            sessionEvent =>
            {
                Assert.Equal("session", sessionEvent.GetProperty("type").GetString());
                Assert.Equal("Chat3:test-session", sessionEvent.GetProperty("sessionId").GetString());
            },
            errorEvent =>
            {
                Assert.Equal("error", errorEvent.GetProperty("type").GetString());
                Assert.Equal("hosted agent boom", errorEvent.GetProperty("errorMessage").GetString());
            });
    }

    private static List<JsonElement> ParseSsePayloads(string body)
    {
        var events = new List<JsonElement>();

        foreach (var block in body.Split("\n\n", StringSplitOptions.RemoveEmptyEntries))
        {
            var line = block.Trim();
            if (!line.StartsWith("data:", StringComparison.Ordinal))
            {
                continue;
            }

            events.Add(JsonDocument.Parse(line["data:".Length..].Trim()).RootElement.Clone());
        }

        return events;
    }
}

public class ChatApiWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IAboutClient>();
            services.AddSingleton<IAboutClient, WeatherApiWebApplicationFactory.StubAboutClient>();

            services.Replace(ServiceDescriptor.KeyedScoped<IChatClientService, StubChatClientService>("Chat1a"));
            services.Replace(ServiceDescriptor.KeyedScoped<IChatClientService, StubChat3ClientService>("Chat3"));
        });
    }
}

internal sealed class StubChatClientService : IChatClientService
{
    public async IAsyncEnumerable<ChatStreamEvent> SendMessageAsync(
        ChatSendMessageRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return ChatStreamEvent.Session("Chat1a:test-session");
        yield return ChatStreamEvent.Token("Hello");
        yield return ChatStreamEvent.Done();
    }
}

internal sealed class StubChat3ClientService : IChatClientService
{
    public async IAsyncEnumerable<ChatStreamEvent> SendMessageAsync(
        ChatSendMessageRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return ChatStreamEvent.Session("Chat3:test-session");
        yield return ChatStreamEvent.Token("Hello from Chat3");
        yield return ChatStreamEvent.Done();
    }
}

public class Chat3ThrowingWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IAboutClient>();
            services.AddSingleton<IAboutClient, WeatherApiWebApplicationFactory.StubAboutClient>();

            services.Replace(ServiceDescriptor.KeyedScoped<IChatClientService, ThrowingAfterSessionChat3Service>("Chat3"));
        });
    }
}

internal sealed class ThrowingAfterSessionChat3Service : IChatClientService
{
    public async IAsyncEnumerable<ChatStreamEvent> SendMessageAsync(
        ChatSendMessageRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return ChatStreamEvent.Session("Chat3:test-session");
        throw new InvalidOperationException("hosted agent boom");
    }
}
