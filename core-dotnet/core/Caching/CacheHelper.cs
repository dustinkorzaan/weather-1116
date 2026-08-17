using Microsoft.Extensions.Caching.Memory;

namespace Core.Caching;

public class CacheHelper
{
    private readonly IMemoryCache _cache;

    public CacheHelper(IMemoryCache cache)
    {
        _cache = cache;
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        TimeSpan duration,
        Func<CancellationToken, Task<T>> factory,
        CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(key, out T? cached))
        {
            return cached!;
        }

        var result = await factory(cancellationToken);
        _cache.Set(key, result, duration);
        return result;
    }
}
