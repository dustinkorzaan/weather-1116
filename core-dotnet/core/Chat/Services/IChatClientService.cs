using Core.Chat.Models;

namespace Core.Chat.Services;

public interface IChatClientService
{
    IAsyncEnumerable<ChatStreamEvent> SendMessageAsync(ChatSendMessageRequest request, CancellationToken cancellationToken);
}
