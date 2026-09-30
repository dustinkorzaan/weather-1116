namespace WeatherBlazor.Data;

// One rendered line in a chat transcript, shared by ChatPanel (/chat-clients) and the
// header Chat2aSidebar so both render through ChatMessageList.
public class ChatEntry
{
    public required string Role { get; set; }
    public required string Content { get; set; }
    public string? ToolName { get; set; }
    public string? ToolArguments { get; set; }
    public string? ToolResult { get; set; }
    public bool Running { get; set; }
    public bool Streaming { get; set; }
    public ChatUsage? Usage { get; set; }
}
