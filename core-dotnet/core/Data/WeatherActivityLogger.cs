using Core.Data.Domain;

namespace Core.Data;

public class WeatherActivityLogger(
    WeatherActivityDbContext dbContext,
    IWeatherActivityHostProvider hostProvider,
    TimeProvider? timeProvider = null) : IWeatherActivityLogger
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public async Task<Guid> LogRequestAsync(
        string feature,
        string featureCategory,
        string sessionId,
        string content,
        string? location = null,
        CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid();

        dbContext.WeatherActivity.Add(new WeatherActivity
        {
            Id = id,
            CorrelationId = id,
            SessionId = sessionId,
            Feature = feature,
            FeatureCategory = featureCategory,
            Host = hostProvider.Host,
            Direction = WeatherActivityDirection.Request,
            CreatedUtc = _timeProvider.GetUtcNow().UtcDateTime,
            Content = content,
            Location = location,
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return id;
    }

    public async Task LogResponseAsync(
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
        CancellationToken cancellationToken = default)
    {
        dbContext.WeatherActivity.Add(new WeatherActivity
        {
            Id = Guid.NewGuid(),
            CorrelationId = correlationId,
            SessionId = sessionId,
            Feature = feature,
            FeatureCategory = featureCategory,
            Host = hostProvider.Host,
            Direction = WeatherActivityDirection.Response,
            CreatedUtc = _timeProvider.GetUtcNow().UtcDateTime,
            Content = content,
            Location = location,
            InputTokenCount = inputTokenCount,
            CachedTokenCount = cachedTokenCount,
            OutputTokenCount = outputTokenCount,
            ReasoningTokenCount = reasoningTokenCount,
            TotalTokenCount = totalTokenCount,
            RuntimeMs = runtimeMs,
            ErrorMessage = errorMessage,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
