namespace Core.Data.Domain;

/// <summary>
/// One row per Request or one row per Response for any prompt/response pair sent to a model,
/// hosted agent, or multi-agent orchestration in this repo -- Chat1a-Chat4b and the three
/// Current AI Weather versions (V3/V4/V5) alike. A Response row's <see cref="CorrelationId"/>
/// equals its paired Request row's <see cref="Id"/>, so a turn's two rows join without needing
/// an update to the Request row once the response is known.
/// </summary>
public class AgentActivity
{
    public Guid Id { get; set; }

    public Guid CorrelationId { get; set; }

    /// <summary>
    /// Shared by every row belonging to one orchestration run -- an orchestrator's own Request/
    /// Response rows and each child agent's (e.g. Chat4a/Chat4b's Geo and NonAI Weather) carry
    /// the same TraceId, so a query can pull the whole multi-agent turn together. CorrelationId
    /// still only ties one row's own Request to its own Response; TraceId is the wider net.
    /// </summary>
    public Guid TraceId { get; set; }

    /// <summary>
    /// Chat session id for Chat1a-Chat4b. Current AI Weather has no real multi-turn session, so
    /// callers generate a fresh GUID per request instead, purely so every row has one.
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>Chat1a, Chat1b, Chat2a, Chat2b, Chat3, Chat4a, Chat4b, AIWeatherV3, AIWeatherV4, AIWeatherV5.</summary>
    public required string Feature { get; set; }

    /// <summary>One of <see cref="AgentActivityFeatureCategory"/>.</summary>
    public required string FeatureCategory { get; set; }

    /// <summary>One of <see cref="AgentActivityHost"/>.</summary>
    public required string Host { get; set; }

    /// <summary>One of <see cref="AgentActivityDirection"/>.</summary>
    public required string Direction { get; set; }

    public DateTime CreatedUtc { get; set; }

    /// <summary>
    /// Which named agent produced this row in a multi-agent orchestration (e.g. "Geo",
    /// "NonAIWeather" in Chat4a/Chat4b). Null for a single-agent row, where <see cref="Feature"/>
    /// already identifies the one agent.
    /// </summary>
    public string? AgentName { get; set; }

    /// <summary>
    /// 1-based tool-call-loop iteration this row belongs to (mirrors AIWeather's own
    /// AIRunLogRecorder loop numbering). Null when the row isn't part of a tool-call loop.
    /// </summary>
    public int? LoopNumber { get; set; }

    /// <summary>Name of the tool invoked, for a nested tool-call row. Null otherwise.</summary>
    public string? ToolName { get; set; }

    /// <summary>Prompt text on a Request row; full response text on a Response row.</summary>
    public string? Content { get; set; }

    /// <summary>Current AI Weather's location query. Null for Chat rows.</summary>
    public string? Location { get; set; }

    public int? InputTokenCount { get; set; }

    public int? CachedTokenCount { get; set; }

    public int? OutputTokenCount { get; set; }

    public int? ReasoningTokenCount { get; set; }

    public int? TotalTokenCount { get; set; }

    /// <summary>Response rows only: elapsed server-side time for the whole turn.</summary>
    public int? RuntimeMs { get; set; }

    /// <summary>Populated on a Response row when the turn failed.</summary>
    public string? ErrorMessage { get; set; }
}
