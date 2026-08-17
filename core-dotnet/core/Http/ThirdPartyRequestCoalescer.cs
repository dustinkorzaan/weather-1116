using System.Collections.Concurrent;

namespace Core.Http;

/// <summary>
/// Ensures at most one fetch per key is in flight at a time. Concurrent callers for the
/// same key await the same in-progress <see cref="Task{TResult}"/> instead of each starting
/// their own upstream request.
/// </summary>
public sealed class ThirdPartyRequestCoalescer
{
    private readonly ConcurrentDictionary<string, Lazy<Task<string>>> _inFlight = new();

    public Task<string> GetOrAdd(string key, Func<Task<string>> fetchAsync)
    {
        var lazy = _inFlight.GetOrAdd(key, _ => new Lazy<Task<string>>(() => RunAndRemove(key, fetchAsync)));
        return lazy.Value;
    }

    private async Task<string> RunAndRemove(string key, Func<Task<string>> fetchAsync)
    {
        try
        {
            return await fetchAsync();
        }
        finally
        {
            _inFlight.TryRemove(key, out _);
        }
    }
}
