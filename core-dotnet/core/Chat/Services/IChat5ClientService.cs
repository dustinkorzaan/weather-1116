using Core.Chat.Models;

namespace Core.Chat.Services;

/// <summary>
/// Chat5a/Chat5b only. Sibling of <see cref="IChatClientService"/> bound to
/// <see cref="Chat5SendMessageRequest"/> instead of <see cref="ChatSendMessageRequest"/>, so
/// Chat1-4's contract and every existing implementation stay untouched.
/// </summary>
public interface IChat5ClientService
{
    IAsyncEnumerable<ChatStreamEvent> SendMessageAsync(Chat5SendMessageRequest request, CancellationToken cancellationToken);
}
