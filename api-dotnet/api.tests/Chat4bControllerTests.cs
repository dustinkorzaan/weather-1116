using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Core.Chat.Models;
using Core.Chat.Services;

namespace WeatherAPI.Tests;

public class Chat4bControllerTests(ChatApiWebApplicationFactory factory) : IClassFixture<ChatApiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task PostMessage_ReturnsCamelCaseSseEvents()
    {
        using var response = await _client.PostAsJsonAsync(
            "/Chat4b/messages",
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
                Assert.Equal("Chat4b:test-session", sessionEvent.GetProperty("sessionId").GetString());
            },
            tokenEvent =>
            {
                Assert.Equal("token", tokenEvent.GetProperty("type").GetString());
                Assert.Equal("Hello from Chat4b", tokenEvent.GetProperty("text").GetString());
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

internal sealed class StubChat4bClientService : IChatClientService
{
    public async IAsyncEnumerable<ChatStreamEvent> SendMessageAsync(
        ChatSendMessageRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return ChatStreamEvent.Session("Chat4b:test-session");
        yield return ChatStreamEvent.Token("Hello from Chat4b");
        yield return ChatStreamEvent.Done();
    }
}
