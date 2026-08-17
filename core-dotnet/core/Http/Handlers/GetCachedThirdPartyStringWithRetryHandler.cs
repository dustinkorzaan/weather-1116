using Core.Http.Events;
using MediatR;
using Microsoft.Extensions.Caching.Memory;

namespace Core.Http.Handlers;

/// <summary>
/// GET helper for Open-Meteo and Nominatim with five retries on failure.
/// Backoff starts at 200ms and doubles after each retry. Successful
/// responses are cached in memory keyed by request URI.
/// </summary>
public class GetCachedThirdPartyStringWithRetryHandler : IRequestHandler<GetCachedThirdPartyStringWithRetryEvent, string>
{
    internal const int RetryCount = 5;
    internal const int RetryDelay = 200;

    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;

    public GetCachedThirdPartyStringWithRetryHandler(HttpClient httpClient, IMemoryCache cache)
    {
        _httpClient = httpClient;
        _cache = cache;
    }

    public async Task<string> Handle(
        GetCachedThirdPartyStringWithRetryEvent request,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.RequestUri);

        if (_cache.TryGetValue(request.RequestUri, out string? cached))
        {
            return cached!;
        }

        for (var attempt = 0; ; attempt++)
        {
            try
            {
                var result = await SendGet(_httpClient, request, cancellationToken);
                _cache.Set(request.RequestUri, result, request.CacheDuration);
                return result;
            }
            catch when (attempt < RetryCount)
            {
                await Task.Delay((int)(RetryDelay * Math.Pow(2, attempt)), cancellationToken);
            }
        }
    }

    private static async Task<string> SendGet(
        HttpClient client,
        GetCachedThirdPartyStringWithRetryEvent request,
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
