using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Core.Chat.Models;
using Core.Chat.Services;

namespace WeatherAPI.Tests;

public class Chat5bControllerTests(ChatApiWebApplicationFactory factory) : IClassFixture<ChatApiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task PostMessage_ReturnsCamelCaseSseEvents()
    {
        using var response = await _client.PostAsJsonAsync(
            "/Chat5b/messages",
            new Chat5SendMessageRequest { Message = "Hi" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);

        var events = ParseSsePayloads(await response.Content.ReadAsStringAsync());
        Assert.Collection(
            events,
            sessionEvent =>
            {
                Assert.Equal("session", sessionEvent.GetProperty("type").GetString());
                Assert.Equal("Chat5b:test-session", sessionEvent.GetProperty("sessionId").GetString());
            },
            tokenEvent =>
            {
                Assert.Equal("token", tokenEvent.GetProperty("type").GetString());
                Assert.Equal("Hello from Chat5b", tokenEvent.GetProperty("text").GetString());
            },
            doneEvent => Assert.Equal("done", doneEvent.GetProperty("type").GetString()));
    }

    [Fact]
    public async Task PostMessage_BlockedRequest_ReturnsBlockedEventNotError()
    {
        using var response = await _client.PostAsJsonAsync(
            "/Chat5b/messages",
            new Chat5SendMessageRequest { Message = "block me" });

        var events = ParseSsePayloads(await response.Content.ReadAsStringAsync());
        var blockedEvent = events.Single(e => e.GetProperty("type").GetString() == "blocked");

        Assert.Equal("Blocked by Code Input: test", blockedEvent.GetProperty("errorMessage").GetString());
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

internal sealed class StubChat5bClientService : IChat5ClientService
{
    public async IAsyncEnumerable<ChatStreamEvent> SendMessageAsync(
        Chat5SendMessageRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return ChatStreamEvent.Session("Chat5b:test-session");

        if (request.Message.Contains("block me", StringComparison.OrdinalIgnoreCase))
        {
            yield return ChatStreamEvent.Blocked("Blocked by Code Input: test");
            yield return ChatStreamEvent.Done();
            yield break;
        }

        yield return ChatStreamEvent.Token("Hello from Chat5b");
        yield return ChatStreamEvent.Done();
    }
}
