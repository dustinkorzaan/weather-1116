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
            doneEvent =>
            {
                Assert.Equal("done", doneEvent.GetProperty("type").GetString());
                var usage = doneEvent.GetProperty("usage");
                Assert.Equal(10, usage.GetProperty("inputTokenCount").GetInt32());
                Assert.Equal(15, usage.GetProperty("totalTokenCount").GetInt32());
                Assert.Equal(42, usage.GetProperty("runtimeMs").GetInt32());
            });

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

public class Chat3UnhandledExceptionTests(ThrowingChat3ApiFactory factory) : IClassFixture<ThrowingChat3ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task PostMessage_WritesErrorEventWhenServiceThrowsAfterSession()
    {
        using var response = await _client.PostAsJsonAsync(
            "/Chat3/messages",
            new ChatSendMessageRequest { Message = "test" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var events = ParseThrowingSse(body);
        Assert.Equal(2, events.Count);
        Assert.Equal("session", events[0].GetProperty("type").GetString());
        Assert.Equal("error", events[1].GetProperty("type").GetString());
        Assert.Equal(
            "HTTP 400: Missing required query parameter: api-version",
            events[1].GetProperty("errorMessage").GetString());
    }

    private static List<JsonElement> ParseThrowingSse(string body)
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

public class ThrowingChat3ApiFactory : ChatApiWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services =>
        {
            services.Replace(ServiceDescriptor.KeyedScoped<IChatClientService, ThrowingChat3ClientService>("Chat3"));
        });
    }
}

internal sealed class ThrowingChat3ClientService : IChatClientService
{
    public async IAsyncEnumerable<ChatStreamEvent> SendMessageAsync(
        ChatSendMessageRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return ChatStreamEvent.Session("Chat3:throw");
        throw new InvalidOperationException("HTTP 400: Missing required query parameter: api-version");
    }
}

public class ChatApiWebApplicationFactory : WebApplicationFactory<Program>
{
    public ChatApiWebApplicationFactory()
    {
        // Set DB_CONNECTION_STRING environment variable before the app starts.
        // This must happen before Program.cs runs Env.TraversePath().Load()
        Environment.SetEnvironmentVariable("DB_CONNECTION_STRING",
            "Server=(localdb)\\mssqllocaldb;Database=WeatherChatTest;Integrated Security=true;",
            EnvironmentVariableTarget.Process);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IAboutClient>();
            services.AddSingleton<IAboutClient, WeatherApiWebApplicationFactory.StubAboutClient>();

            services.Replace(ServiceDescriptor.KeyedScoped<IChatClientService, StubChatClientService>("Chat1a"));
            services.Replace(ServiceDescriptor.KeyedScoped<IChatClientService, StubChat3ClientService>("Chat3"));
            services.Replace(ServiceDescriptor.KeyedScoped<IChatClientService, StubChat4aClientService>("Chat4a"));
            services.Replace(ServiceDescriptor.KeyedScoped<IChatClientService, StubChat4bClientService>("Chat4b"));
            services.Replace(ServiceDescriptor.KeyedScoped<IChat5ClientService, StubChat5aClientService>("Chat5a"));
            services.Replace(ServiceDescriptor.KeyedScoped<IChat5ClientService, StubChat5bClientService>("Chat5b"));
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
        yield return ChatStreamEvent.Done(new ChatUsage
        {
            InputTokenCount = 10,
            OutputTokenCount = 5,
            TotalTokenCount = 15,
            RuntimeMs = 42,
        });
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
