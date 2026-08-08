using System.Collections.Concurrent;
using Microsoft.Agents.AI;

namespace Core.Chat;

public sealed class ChatAgentSessionStore
{
    private readonly ConcurrentDictionary<string, Lazy<Task<AgentSession>>> _sessions = new(StringComparer.Ordinal);

    public async Task<AgentSession> GetOrCreateAsync(
        AIAgent agent,
        string sessionId,
        CancellationToken cancellationToken)
    {
        var lazySession = _sessions.GetOrAdd(
            sessionId,
            _ => new Lazy<Task<AgentSession>>(() => agent.CreateSessionAsync(cancellationToken: cancellationToken).AsTask()));

        try
        {
            return await lazySession.Value;
        }
        catch
        {
            // Do not cache a failed creation attempt.
            _sessions.TryRemove(new KeyValuePair<string, Lazy<Task<AgentSession>>>(sessionId, lazySession));
            throw;
        }
    }
}
