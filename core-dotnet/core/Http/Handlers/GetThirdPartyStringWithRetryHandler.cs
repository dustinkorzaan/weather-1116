using System.Net.Sockets;
using Core.Http.Events;
using MediatR;

namespace Core.Http.Handlers;

/// <summary>
/// GET helper for Open-Meteo and Nominatim with five retries on transient HTTPS failures.
/// Backoff starts at 200ms and doubles after each retry.
/// </summary>
public class GetThirdPartyStringWithRetryHandler : IRequestHandler<GetThirdPartyStringWithRetryEvent, string>
{
    internal const int RetryCount = 5;
    internal static readonly TimeSpan InitialRetryDelay = TimeSpan.FromMilliseconds(200);

    private readonly HttpClient _httpClient;
    private readonly Func<TimeSpan, CancellationToken, Task> _delayAsync;

    public GetThirdPartyStringWithRetryHandler(HttpClient httpClient)
        : this(httpClient, Task.Delay)
    {
    }

    internal GetThirdPartyStringWithRetryHandler(
        HttpClient httpClient,
        Func<TimeSpan, CancellationToken, Task> delayAsync)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(delayAsync);
        _httpClient = httpClient;
        _delayAsync = delayAsync;
    }

    public Task<string> Handle(
        GetThirdPartyStringWithRetryEvent request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.RequestUri);

        return GetStringWithRetryAsync(request, cancellationToken);
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
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                return await SendGetAsync(_httpClient, request, cancellationToken);
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
