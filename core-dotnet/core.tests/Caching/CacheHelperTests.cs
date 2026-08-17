using Core.Caching;
using Microsoft.Extensions.Caching.Memory;

namespace Core.Tests.Caching;

public class CacheHelperTests
{
    [Fact]
    public async Task GetOrCreate_CacheMiss_InvokesFactoryAndStoresResult()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new CacheHelper(cache);
        var calls = 0;

        var result = await sut.GetOrCreate(
            "key",
            TimeSpan.FromMinutes(5),
            _ =>
            {
                calls++;
                return Task.FromResult("value");
            },
            CancellationToken.None);

        Assert.Equal("value", result);
        Assert.Equal(1, calls);
        Assert.True(cache.TryGetValue("key", out string? cached));
        Assert.Equal("value", cached);
    }

    [Fact]
    public async Task GetOrCreate_CacheHit_DoesNotInvokeFactoryAgain()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new CacheHelper(cache);
        var calls = 0;

        Task<string> Factory(CancellationToken _)
        {
            calls++;
            return Task.FromResult("value");
        }

        var first = await sut.GetOrCreate("key", TimeSpan.FromMinutes(5), Factory, CancellationToken.None);
        var second = await sut.GetOrCreate("key", TimeSpan.FromMinutes(5), Factory, CancellationToken.None);

        Assert.Equal("value", first);
        Assert.Equal("value", second);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task GetOrCreate_DifferentKeys_InvokeFactorySeparately()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new CacheHelper(cache);
        var calls = 0;

        Task<string> Factory(CancellationToken _)
        {
            calls++;
            return Task.FromResult($"value-{calls}");
        }

        var first = await sut.GetOrCreate("key-1", TimeSpan.FromMinutes(5), Factory, CancellationToken.None);
        var second = await sut.GetOrCreate("key-2", TimeSpan.FromMinutes(5), Factory, CancellationToken.None);

        Assert.Equal("value-1", first);
        Assert.Equal("value-2", second);
        Assert.Equal(2, calls);
    }
}
