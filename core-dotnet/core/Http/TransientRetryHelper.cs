using System.Net.Http.Headers;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace Core.Http;

public class TransientRetryHelper
{
    internal const int RetryCount = 5;
    internal const int RetryDelay = 200;

    private readonly ILogger<TransientRetryHelper> _logger;

    public TransientRetryHelper(ILogger<TransientRetryHelper> logger)
    {
        _logger = logger;
    }

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                return await action(cancellationToken);
            }
            catch (Exception ex) when (attempt < RetryCount && IsTransient(ex, cancellationToken))
            {
                var delay = ex is RetryAfterException retryAfter
                    ? retryAfter.RetryAfter
                    : TimeSpan.FromMilliseconds(RetryDelay * Math.Pow(2, attempt));

                _logger.LogWarning(
                    ex,
                    "Transient failure on attempt {Attempt}/{RetryCount}, retrying in {Delay}",
                    attempt + 1,
                    RetryCount,
                    delay);
                await Task.Delay(delay, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Like <see cref="HttpResponseMessage.EnsureSuccessStatusCode"/>, but throws a
    /// <see cref="RetryAfterException"/> carrying the server's requested delay when the
    /// response includes a Retry-After header (typically on 429 or 503).
    /// </summary>
    public static void EnsureSuccessOrThrowRetryAfter(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        if (response.Headers.RetryAfter is RetryConditionHeaderValue retryAfter)
        {
            var delay = retryAfter.Delta ?? retryAfter.Date - DateTimeOffset.UtcNow;
            if (delay is { } positiveDelay && positiveDelay > TimeSpan.Zero)
            {
                throw new RetryAfterException(
                    $"Non-AI: {(int)response.StatusCode} {response.ReasonPhrase} with Retry-After {positiveDelay}.",
                    positiveDelay);
            }
        }

        response.EnsureSuccessStatusCode();
    }

    private static bool IsTransient(Exception exception, CancellationToken cancellationToken) =>
        !cancellationToken.IsCancellationRequested
        && exception is HttpRequestException or IOException or SocketException or TaskCanceledException;
}
