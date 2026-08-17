using System.Net.Sockets;
using Core.Http.Events;
using MediatR;
using Microsoft.Extensions.Caching.Memory;

namespace Core.Http.Handlers;

/// <summary>
/// GET helper for Open-Meteo and Nominatim with five retries on transient HTTPS failures.
/// Backoff starts at 200ms and doubles after each retry.
/// Successful response bodies are cached per process in <see cref="IMemoryCache"/> by request URI.
/// Failures are not cached.
/// </summary>
public class GetThirdPartyStringWithRetryHandler : IRequestHandler<GetThirdPartyStringWithRetryEvent, string>
{
    internal const int RetryCount = 5;
    internal static readonly TimeSpan InitialRetryDelay = TimeSpan.FromMilliseconds(200);
    internal static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private readonly IMemoryCache _cache;
    private readonly HttpClient? _httpClient;
    private readonly Func<TimeSpan, CancellationToken, Task> _delayAsync;

    public GetThirdPartyStringWithRetryHandler(IMemoryCache cache)
        : this(cache, httpClient: null, Task.Delay)
    {
    }

    internal GetThirdPartyStringWithRetryHandler(
        IMemoryCache cache,
        HttpClient? httpClient,
        Func<TimeSpan, CancellationToken, Task> delayAsync)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(delayAsync);
        _cache = cache;
        _httpClient = httpClient;
        _delayAsync = delayAsync;
    }

    public async Task<string> Handle(
        GetThirdPartyStringWithRetryEvent request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.RequestUri);

        if (_cache.TryGetValue(request.RequestUri, out string? cached) && cached is not null)
        {
            return cached;
        }

        var body = await GetStringWithRetryAsync(request, cancellationToken);
        _cache.Set(request.RequestUri, body, CacheDuration);
        return body;
    }

    internal static TimeSpan DelayBeforeRetry(int retryIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(retryIndex);
        return TimeSpan.FromMilliseconds(InitialRetryDelay.TotalMilliseconds * Math.Pow(2, retryIndex));
    }

    private async Task<string> GetStringWithRetryAsync(
        GetThirdPartyStringWithRetryEvent request,
        CancellationToken cancellationToken)
    {
        var ownsClient = _httpClient is null;
        using HttpClient? ownedClient = ownsClient ? new HttpClient() : null;
        var client = ownedClient ?? _httpClient!;

        for (var attempt = 0; ; attempt++)
        {
            try
            {
                return await SendGetAsync(client, request, cancellationToken);
            }
            catch (Exception exception) when (
                IsTransient(exception, cancellationToken) && attempt < RetryCount)
            {
                await _delayAsync(DelayBeforeRetry(attempt), cancellationToken);
            }
        }
    }

    private static async Task<string> SendGetAsync(
        HttpClient client,
        GetThirdPartyStringWithRetryEvent request,
        CancellationToken cancellationToken)
    {
        if (request.Headers is not { Count: > 0 })
        {
            return await client.GetStringAsync(request.RequestUri, cancellationToken);
        }

        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, request.RequestUri);
        foreach (var header in request.Headers)
        {
            httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        using var response = await client.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private static bool IsTransient(Exception exception, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        return exception is HttpRequestException or IOException or SocketException;
    }
}
