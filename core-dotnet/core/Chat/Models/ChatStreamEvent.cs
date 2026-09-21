namespace Core.Chat.Models;

public class ChatStreamEvent
{
    public required string Type { get; init; }

    public string? SessionId { get; init; }

    public string? Text { get; init; }

    public string? ToolName { get; init; }

    public string? ToolArguments { get; init; }

    public string? ToolResult { get; init; }

    public string? ErrorMessage { get; init; }

    public ChatUsage? Usage { get; init; }

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

    public static ChatStreamEvent ToolStart(string toolName, string? toolArguments = null) => new()
    {
        Type = "tool_start",
        ToolName = toolName,
        ToolArguments = toolArguments,
    };

    public static ChatStreamEvent ToolEnd(string toolName, string? toolArguments = null, string? toolResult = null) => new()
    {
        Type = "tool_end",
        ToolName = toolName,
        ToolArguments = toolArguments,
        ToolResult = toolResult,
    };

    public static ChatStreamEvent Done(ChatUsage? usage = null) => new()
    {
        Type = "done",
        Usage = usage,
    };

    public static ChatStreamEvent Error(string message) => new()
    {
        Type = "error",
        ErrorMessage = message,
    };

    /// <summary>
    /// Chat5a/Chat5b only: a guardrail gate refused the request. Distinct from
    /// <see cref="Error"/> so the UI can style "a gate refused this" differently from a
    /// real failure.
    /// </summary>
    public static ChatStreamEvent Blocked(string message) => new()
    {
        Type = "blocked",
        ErrorMessage = message,
    };
}
