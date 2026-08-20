using System.Text.Json;
using Core.Chat.Models;

namespace Core.Tests.Chat;

public class ChatStreamEventSerializerTests
{
    [Fact]
    public void Serialize_UsesCamelCasePropertyNames()
    {
        var json = ChatStreamEventSerializer.Serialize(ChatStreamEvent.ToolStart("get_public_weather_data"));

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.True(root.TryGetProperty("type", out var type));
        Assert.Equal("tool_start", type.GetString());

        Assert.True(root.TryGetProperty("toolName", out var toolName));
        Assert.Equal("get_public_weather_data", toolName.GetString());

        Assert.False(root.TryGetProperty("Type", out _));
        Assert.False(root.TryGetProperty("ToolName", out _));
        Assert.False(root.TryGetProperty("toolArguments", out _));
        Assert.False(root.TryGetProperty("toolResult", out _));
    }

    [Fact]
    public void Serialize_ToolEndEvent_IncludesArgumentsAndResult()
    {
        var json = ChatStreamEventSerializer.Serialize(
            ChatStreamEvent.ToolEnd(
                "GetLatLong",
                """{"location":"Nashville, TN"}""",
                """[{"name":"Nashville"}]"""));

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal("tool_end", root.GetProperty("type").GetString());
        Assert.Equal("GetLatLong", root.GetProperty("toolName").GetString());
        Assert.Equal("""{"location":"Nashville, TN"}""", root.GetProperty("toolArguments").GetString());
        Assert.Equal("""[{"name":"Nashville"}]""", root.GetProperty("toolResult").GetString());
    }

    [Fact]
    public void Serialize_SessionEvent_UsesCamelCaseSessionId()
    {
        var json = ChatStreamEventSerializer.Serialize(ChatStreamEvent.Session("Chat1a:abc123"));

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.True(root.TryGetProperty("sessionId", out var sessionId));
        Assert.Equal("Chat1a:abc123", sessionId.GetString());
    }

    [Fact]
    public void Serialize_DoneWithoutUsage_OmitsUsageProperty()
    {
        var json = ChatStreamEventSerializer.Serialize(ChatStreamEvent.Done());

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal("done", root.GetProperty("type").GetString());
        Assert.False(root.TryGetProperty("usage", out _));
    }

    [Fact]
    public void Serialize_DoneWithUsage_UsesCamelCaseNestedProperties()
    {
        var json = ChatStreamEventSerializer.Serialize(ChatStreamEvent.Done(new ChatUsage
        {
            InputTokenCount = 3100,
            CachedTokenCount = 200,
            OutputTokenCount = 1118,
            ReasoningTokenCount = 40,
            TotalTokenCount = 4218,
            RuntimeMs = 1240,
        }));

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var usage = root.GetProperty("usage");

        Assert.Equal("done", root.GetProperty("type").GetString());
        Assert.Equal(3100, usage.GetProperty("inputTokenCount").GetInt32());
        Assert.Equal(200, usage.GetProperty("cachedTokenCount").GetInt32());
        Assert.Equal(1118, usage.GetProperty("outputTokenCount").GetInt32());
        Assert.Equal(40, usage.GetProperty("reasoningTokenCount").GetInt32());
        Assert.Equal(4218, usage.GetProperty("totalTokenCount").GetInt32());
        Assert.Equal(1240, usage.GetProperty("runtimeMs").GetInt32());
        Assert.False(usage.TryGetProperty("InputTokenCount", out _));
    }
}
