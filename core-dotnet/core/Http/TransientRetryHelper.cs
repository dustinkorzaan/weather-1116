using System.Net.Sockets;
using System.Text.Json;
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

    public async Task<T> Execute<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                return await action(cancellationToken);
            }
            catch (Exception ex) when (attempt < RetryCount && IsTransient(ex, cancellationToken))
            {
                _logger.LogWarning(
                    ex,
                    "Transient failure on attempt {Attempt}/{RetryCount}, retrying",
                    attempt + 1,
                    RetryCount);
                await Task.Delay((int)(RetryDelay * Math.Pow(2, attempt)), cancellationToken);
            }
        }
    }

    private static bool IsTransient(Exception exception, CancellationToken cancellationToken) =>
        !cancellationToken.IsCancellationRequested
        && exception is HttpRequestException or IOException or SocketException or JsonException;
}
