namespace Core.Data;

/// <summary>
/// Writes dbo.WeatherActivity rows for a prompt/response pair -- one call at the start of a
/// turn, one at the end. Both are required, not best-effort: DB_CONNECTION_STRING is a hard
/// requirement in every host, and a write failure is expected to propagate and fail the request
/// rather than be swallowed.
/// </summary>
public interface IWeatherActivityLogger
{
    /// <summary>Logs the Request row and returns its Id, to be passed back as <paramref name="correlationId"/> on <see cref="LogResponseAsync"/>.</summary>
    Task<Guid> LogRequestAsync(
        string feature,
        string featureCategory,
        string sessionId,
        string content,
        string? location = null,
        CancellationToken cancellationToken = default);

    Task LogResponseAsync(
        Guid correlationId,
        string feature,
        string featureCategory,
        string sessionId,
        string? content,
        string? location = null,
        int? inputTokenCount = null,
        int? cachedTokenCount = null,
        int? outputTokenCount = null,
        int? reasoningTokenCount = null,
        int? totalTokenCount = null,
        int? runtimeMs = null,
        string? errorMessage = null,
        CancellationToken cancellationToken = default);
}
