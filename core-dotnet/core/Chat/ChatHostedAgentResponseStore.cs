using System.Collections.Concurrent;

namespace Core.Chat;

/// <summary>
/// Remembers the Foundry conversation id per Chat3 session so later turns
/// continue the same conversation. Foundry rejects sending both
/// <c>conversation</c> and <c>previous_response_id</c>, and the conversation
/// already carries prior turns, so no response id is tracked.
/// </summary>
public sealed class ChatHostedAgentResponseStore
{
    private readonly ConcurrentDictionary<string, string> _conversationIds = new(StringComparer.Ordinal);

    public string? GetConversationId(string sessionId)
        => _conversationIds.TryGetValue(sessionId, out var conversationId) ? conversationId : null;

    public void SetConversationId(string sessionId, string conversationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);
        _conversationIds[sessionId] = conversationId;
    }
}
