using Microsoft.Extensions.Caching.Memory;

namespace Core.Caching;

public class CacheHelper
{
    private readonly IMemoryCache _cache;

    public CacheHelper(IMemoryCache cache)
    {
        _cache = cache;
    }

    public async Task<T> GetOrCreate<T>(
        string cacheKey,
        TimeSpan cacheDuration,
        Func<CancellationToken, Task<T>> valueFactory,
        CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(cacheKey, out T? cached))
        {
            return cached!;
        }

        var result = await valueFactory(cancellationToken);
        _cache.Set(cacheKey, result, cacheDuration);
        return result;
    }
}
