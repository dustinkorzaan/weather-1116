namespace Core.Chat.Models;

public class ChatStreamEvent
{
    public required string Type { get; init; }

    public string? SessionId { get; init; }

    public string? Text { get; init; }

    public string? ToolName { get; init; }

    public string? ErrorMessage { get; init; }

    public static ChatStreamEvent Session(string sessionId) => new()
    {
        Type = "session",
        SessionId = sessionId,
    };

    public static ChatStreamEvent Token(string text) => new()
    {
        Type = "token",
        Text = text,
    };

    public static ChatStreamEvent ToolStart(string toolName) => new()
    {
        Type = "tool_start",
        ToolName = toolName,
    };

    public static ChatStreamEvent ToolEnd(string toolName) => new()
    {
        Type = "tool_end",
        ToolName = toolName,
    };

    public static ChatStreamEvent Done() => new()
    {
        Type = "done",
    };

    public static ChatStreamEvent Error(string message) => new()
    {
        Type = "error",
        ErrorMessage = message,
    };
}
