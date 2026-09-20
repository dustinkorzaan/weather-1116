using CQMediator;

namespace Core.Agent.Events;

/// <summary>
/// Logs one dbo.AgentActivity row (a Request or a Response -- see
/// <see cref="Core.Data.Domain.AgentActivityDirection"/>) and returns its Id.
///
/// Two ids, two different jobs: send once per Request with <see cref="CorrelationId"/> left
/// null -- the returned Id is *that Request's own* CorrelationId, to pass back in on its own
/// paired Response send only. A nested tool-call or child-agent row is a separate Request/
/// Response pair with its own freshly-minted CorrelationId (from its own Request send), not the
/// parent turn's. <see cref="RunId"/> is what ties the whole thing together: it is the same
/// value on the parent turn's rows and on every nested row underneath it.
/// </summary>
public class LogAgentActivityEvent : IRequest<Guid>
{
    public Guid? RunId { get; init; }

    public Guid? CorrelationId { get; init; }

    public string? SessionId { get; init; }

    public string? Direction { get; init; }

    public string? Feature { get; init; }

    public string? FeatureCategory { get; init; }

    public string? AgentName { get; init; }

    public int? LoopNumber { get; init; }

    public string? ToolName { get; init; }

    public string? Content { get; init; }

    public string? Location { get; init; }

    public int? InputTokenCount { get; init; }

    public int? CachedTokenCount { get; init; }

    public int? OutputTokenCount { get; init; }

    public int? ReasoningTokenCount { get; init; }

    public int? TotalTokenCount { get; init; }

    public int? RuntimeMs { get; init; }

    public string? ErrorMessage { get; init; }
}
