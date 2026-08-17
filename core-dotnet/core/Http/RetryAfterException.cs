namespace Core.Http;

/// <summary>
/// A transient HTTP failure that carries the server's requested Retry-After delay,
/// so TransientRetryHelper waits that long instead of its default backoff.
/// </summary>
public class RetryAfterException : HttpRequestException
{
    public TimeSpan RetryAfter { get; }

    public RetryAfterException(string message, TimeSpan retryAfter)
        : base(message)
    {
        RetryAfter = retryAfter;
    }
}
