using System.Net.Sockets;

namespace Core.Http;

/// <summary>
/// GET helper for Open-Meteo and Nominatim with three retries on transient HTTPS failures.
/// </summary>
internal static class ThirdPartyHttp
{
    internal static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromMilliseconds(500),
        TimeSpan.FromMilliseconds(1000),
        TimeSpan.FromMilliseconds(2000),
    ];

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
                IsTransient(exception, cancellationToken) && attempt < RetryDelays.Length)
            {
                await delayAsync(RetryDelays[attempt], cancellationToken);
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
