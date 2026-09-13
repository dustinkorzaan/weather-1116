using CQMediator;

namespace Core.Agent.Events;

/// <summary>
/// Logs one dbo.AgentActivity row (a Request or a Response -- see
/// <see cref="Core.Data.Domain.AgentActivityDirection"/>) and returns its Id. Send it once per Request with
/// <see cref="CorrelationId"/> left null; the returned Id is that turn's CorrelationId, to pass
/// back in on the paired Response send (and on any nested tool-call or child-agent rows that
/// belong to the same Request/Response pair). <see cref="TraceId"/> is the wider id shared by
/// every row in one orchestration run, parent and child agents alike.
/// </summary>
public class LogAgentActivityEvent : IRequest<Guid>
{
    public required string Direction { get; init; }

    public required Guid TraceId { get; init; }

    /// <summary>Null on a Request row (the new row's own Id becomes the CorrelationId); the Request row's Id on its paired Response row.</summary>
    public Guid? CorrelationId { get; init; }

    public required string Feature { get; init; }

    public required string FeatureCategory { get; init; }

    public required string SessionId { get; init; }

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
