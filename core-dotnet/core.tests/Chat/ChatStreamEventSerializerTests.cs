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
}
