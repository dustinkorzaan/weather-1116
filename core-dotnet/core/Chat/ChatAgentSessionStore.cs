using System.Collections.Concurrent;
using Microsoft.Agents.AI;

namespace Core.Chat;

public sealed class ChatAgentSessionStore
{
    private readonly ConcurrentDictionary<string, AgentSession> _sessions = new(StringComparer.Ordinal);

    public async Task<AgentSession> GetOrCreateAsync(
        AIAgent agent,
        string sessionId,
        CancellationToken cancellationToken)
    {
        if (_sessions.TryGetValue(sessionId, out var existing))
        {
            return existing;
        }

        var created = await agent.CreateSessionAsync(cancellationToken: cancellationToken);
        _sessions[sessionId] = created;
        return created;
    }
}
