using System.Collections.Concurrent;

namespace Core.Http;

/// <summary>
/// One in-flight GET per key. Extra callers wait on that GET instead of starting another.
/// If the last waiter gives up, the in-flight GET is canceled. If anyone is still waiting,
/// the GET keeps running for them.
/// </summary>
public sealed class ThirdPartyRequestCoalescer
{
    private readonly ConcurrentDictionary<string, InFlight> _inFlight = new();

    public async Task<string> GetOrAdd(
        string key,
        Func<CancellationToken, Task<string>> fetchAsync,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(fetchAsync);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var entry = _inFlight.GetOrAdd(key, _ => Create(fetchAsync));
            Interlocked.Increment(ref entry.Waiters);

            if (!_inFlight.TryGetValue(key, out var current) || !ReferenceEquals(current, entry))
            {
                Leave(key, entry);
                continue;
            }

            try
            {
                return await entry.Work.Value.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // Shared GET was canceled because every other waiter left. This caller still
                // wants a result, so join or start a new fetch.
            }
            finally
            {
                Leave(key, entry);
            }
        }
    }

    private static InFlight Create(Func<CancellationToken, Task<string>> fetchAsync)
    {
        var cts = new CancellationTokenSource();
        return new InFlight
        {
            Cts = cts,
            Work = new Lazy<Task<string>>(() => fetchAsync(cts.Token)),
        };
    }

    private void Leave(string key, InFlight entry)
    {
        if (Interlocked.Decrement(ref entry.Waiters) != 0)
        {
            return;
        }

        _inFlight.TryRemove(KeyValuePair.Create(key, entry));

        if (entry.Work.IsValueCreated && entry.Work.Value.IsCompleted)
        {
            return;
        }

        try
        {
            entry.Cts.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private sealed class InFlight
    {
        public required Lazy<Task<string>> Work { get; init; }

        public required CancellationTokenSource Cts { get; init; }

        public int Waiters;
    }
}
