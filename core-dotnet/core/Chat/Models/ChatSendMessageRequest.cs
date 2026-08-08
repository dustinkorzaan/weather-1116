namespace Core.Chat.Models;

public class ChatSendMessageRequest
{
    public string? SessionId { get; set; }

    public required string Message { get; set; }
}
