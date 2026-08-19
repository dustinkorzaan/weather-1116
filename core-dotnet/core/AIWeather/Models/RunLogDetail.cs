using System.Text.Json.Serialization;

namespace Core.AIWeather.Models;

public class RunLogDetail
{
    [JsonPropertyName("dateTimeUtc")]
    public DateTime DateTimeUtc { get; set; }

    [JsonPropertyName("loopNumber")]
    public int LoopNumber { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("inputTokenCount")]
    public int? InputTokenCount { get; set; }

    [JsonPropertyName("cachedTokenCount")]
    public int? CachedTokenCount { get; set; }

    [JsonPropertyName("outputTokenCount")]
    public int? OutputTokenCount { get; set; }

    [JsonPropertyName("reasoningTokenCount")]
    public int? ReasoningTokenCount { get; set; }

    [JsonPropertyName("totalTokenCount")]
    public int? TotalTokenCount { get; set; }

    [JsonPropertyName("runtimeMs")]
    public int RuntimeMs { get; set; }

    [JsonPropertyName("loopRuntimeMs")]
    public int LoopRuntimeMs { get; set; }

    [JsonPropertyName("runningTotalMs")]
    public int RunningTotalMs { get; set; }
}
