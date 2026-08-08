using Core.Chat.Models;

namespace Core.Chat.Services;

public interface IChatSessionStore
{
    string CreateSession(string chatKind);

    IReadOnlyList<ChatMessage> GetMessages(string sessionId);

    void AppendMessage(string sessionId, ChatMessage message);

    bool SessionExists(string sessionId);
}
