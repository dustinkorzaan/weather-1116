using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Core.Chat.Models;
using Core.Chat.Services;

namespace WeatherAPI.Tests;

public class Chat5aControllerTests(ChatApiWebApplicationFactory factory) : IClassFixture<ChatApiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task PostMessage_DefaultGates_ReturnsCamelCaseSseEvents()
    {
        using var response = await _client.PostAsJsonAsync(
            "/Chat5a/messages",
            new Chat5SendMessageRequest { Message = "Hi" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);

        var events = ParseSsePayloads(await response.Content.ReadAsStringAsync());
        Assert.Collection(
            events,
            sessionEvent =>
            {
                Assert.Equal("session", sessionEvent.GetProperty("type").GetString());
                Assert.Equal("Chat5a:test-session", sessionEvent.GetProperty("sessionId").GetString());
            },
            tokenEvent =>
            {
                Assert.Equal("token", tokenEvent.GetProperty("type").GetString());
                Assert.Equal(
                    "gates: maxLength=True ruleInput=True llmInput=True systemPrompt=True llmOutput=True",
                    tokenEvent.GetProperty("text").GetString());
            },
            doneEvent => Assert.Equal("done", doneEvent.GetProperty("type").GetString()));
    }

    [Fact]
    public async Task PostMessage_ExplicitGateFlags_RoundTripToTheService()
    {
        using var response = await _client.PostAsJsonAsync(
            "/Chat5a/messages",
            new Chat5SendMessageRequest
            {
                Message = "Hi",
                EnableRuleInputGate = false,
                EnableLlmOutputGate = false,
            });

        var events = ParseSsePayloads(await response.Content.ReadAsStringAsync());
        var tokenEvent = events.Single(e => e.GetProperty("type").GetString() == "token");

        Assert.Equal(
            "gates: maxLength=True ruleInput=False llmInput=True systemPrompt=True llmOutput=False",
            tokenEvent.GetProperty("text").GetString());
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

internal sealed class StubChat5aClientService : IChat5ClientService
{
    public async IAsyncEnumerable<ChatStreamEvent> SendMessageAsync(
        Chat5SendMessageRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return ChatStreamEvent.Session("Chat5a:test-session");
        yield return ChatStreamEvent.Token(
            $"gates: maxLength={request.EnableMaxLengthGate} ruleInput={request.EnableRuleInputGate} " +
            $"llmInput={request.EnableLlmInputGate} systemPrompt={request.EnableSystemPromptGuard} " +
            $"llmOutput={request.EnableLlmOutputGate}");
        yield return ChatStreamEvent.Done();
    }
}
