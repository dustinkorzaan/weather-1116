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
