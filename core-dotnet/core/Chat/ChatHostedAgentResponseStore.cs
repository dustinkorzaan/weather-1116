using System.Collections.Concurrent;

namespace Core.Chat;

/// <summary>
/// Remembers the last Foundry Responses id per Chat3 session so later turns
/// can send <c>previous_response_id</c> instead of replaying a system prompt.
/// </summary>
public sealed class ChatHostedAgentResponseStore
{
    private readonly ConcurrentDictionary<string, string> _previousResponseIds = new(StringComparer.Ordinal);

    public string? GetPreviousResponseId(string sessionId)
        => _previousResponseIds.TryGetValue(sessionId, out var responseId) ? responseId : null;

    public void SetPreviousResponseId(string sessionId, string responseId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(responseId);
        _previousResponseIds[sessionId] = responseId;
    }
}
