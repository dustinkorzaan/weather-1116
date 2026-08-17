using System.Collections.Concurrent;
using Core.Http.Events;
using MediatR;

namespace Core.Http.Handlers;

/// <summary>
/// GET helper for Open-Meteo and Nominatim with five retries on failure.
/// Backoff starts at 200ms and doubles after each retry. Successful
/// responses are cached in memory keyed by request URI.
/// </summary>
public class GetThirdPartyStringWithRetryHandler : IRequestHandler<GetThirdPartyStringWithRetryEvent, string>
{
    internal const int RetryCount = 5;
    internal const int RetryDelay = 200;

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);
    private static readonly ConcurrentDictionary<string, (string Value, DateTimeOffset CachedAt)> Cache = new();

    private readonly HttpClient _httpClient;

    public GetThirdPartyStringWithRetryHandler(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> Handle(
        GetThirdPartyStringWithRetryEvent request,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.RequestUri);

        var cached = GetFromCache(request.RequestUri);
        if (!string.IsNullOrWhiteSpace(cached))
        {
            return cached;
        }

        for (var attempt = 0; ; attempt++)
        {
            try
            {
                var result = await SendGetAsync(_httpClient, request, cancellationToken);
                SaveToCache(request.RequestUri, result);
                return result;
            }
            catch when (attempt < RetryCount)
            {
                await Task.Delay((int)(RetryDelay * Math.Pow(2, attempt)), cancellationToken);
            }
        }
    }

    private static string? GetFromCache(string requestUri)
    {
        if (Cache.TryGetValue(requestUri, out var entry) && DateTimeOffset.UtcNow - entry.CachedAt > CacheDuration)
        {
            Cache.TryRemove(requestUri, out _);
        }

        return Cache.TryGetValue(requestUri, out var current) ? current.Value : null;
    }

    private static void SaveToCache(string requestUri, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        Cache.TryAdd(requestUri, (value, DateTimeOffset.UtcNow));
    }

    private static async Task<string> SendGetAsync(
        HttpClient client,
        GetThirdPartyStringWithRetryEvent request,
        CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, request.RequestUri);
        foreach (var header in request.Headers)
        {
            httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        using var response = await client.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}
