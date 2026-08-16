using System.Net.Sockets;

namespace Core.Http;

/// <summary>
/// GET helper for Open-Meteo and Nominatim with five retries on transient HTTPS failures.
/// Backoff starts at 200ms and doubles after each retry.
/// </summary>
internal static class ThirdPartyHttp
{
    internal const int RetryCount = 5;
    internal static readonly TimeSpan InitialRetryDelay = TimeSpan.FromMilliseconds(200);

    internal static TimeSpan DelayBeforeRetry(int retryIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(retryIndex);
        return TimeSpan.FromMilliseconds(InitialRetryDelay.TotalMilliseconds * Math.Pow(2, retryIndex));
    }

    internal static Task<string> GetStringWithRetryAsync(
        HttpClient client,
        string requestUri,
        CancellationToken cancellationToken) =>
        GetStringWithRetryAsync(client, requestUri, Task.Delay, cancellationToken);

    internal static async Task<string> GetStringWithRetryAsync(
        HttpClient client,
        string requestUri,
        Func<TimeSpan, CancellationToken, Task> delayAsync,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(delayAsync);

        for (var attempt = 0; ; attempt++)
        {
            try
            {
                return await client.GetStringAsync(requestUri, cancellationToken);
            }
            catch (Exception exception) when (
                IsTransient(exception, cancellationToken) && attempt < RetryCount)
            {
                await delayAsync(DelayBeforeRetry(attempt), cancellationToken);
            }
        }
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
