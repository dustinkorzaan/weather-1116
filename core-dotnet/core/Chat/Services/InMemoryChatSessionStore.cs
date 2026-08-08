using System.Collections.Concurrent;
using Core.Chat.Models;

namespace Core.Chat.Services;

public sealed class InMemoryChatSessionStore : IChatSessionStore
{
    private readonly ConcurrentDictionary<string, List<ChatMessage>> _sessions = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, string> _sessionKinds = new(StringComparer.Ordinal);

    public string CreateSession(string chatKind)
    {
        var sessionId = $"{chatKind}:{Guid.NewGuid():N}";
        _sessions[sessionId] = [];
        _sessionKinds[sessionId] = chatKind;
        return sessionId;
    }

    public IReadOnlyList<ChatMessage> GetMessages(string sessionId)
    {
        return _sessions.TryGetValue(sessionId, out var messages)
            ? messages.AsReadOnly()
            : Array.Empty<ChatMessage>();
    }

    public void AppendMessage(string sessionId, ChatMessage message)
    {
        if (!_sessions.ContainsKey(sessionId))
        {
            throw new KeyNotFoundException($"Chat session '{sessionId}' was not found.");
        }

        _sessions[sessionId].Add(message);
    }

    public bool SessionExists(string sessionId) => _sessions.ContainsKey(sessionId);
}
