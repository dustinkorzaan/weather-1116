using System.Net.Sockets;

namespace Core.Http;

public class TransientRetryHelper
{
    internal const int RetryCount = 5;
    internal const int RetryDelay = 200;

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
                await Task.Delay((int)(RetryDelay * Math.Pow(2, attempt)), cancellationToken);
            }
        }
    }

    private static bool IsTransient(Exception exception, CancellationToken cancellationToken) =>
        !cancellationToken.IsCancellationRequested
        && exception is HttpRequestException or IOException or SocketException;
}
