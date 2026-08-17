using Microsoft.Extensions.DependencyInjection;

namespace Core.Http;

public static class HttpServiceCollectionExtensions
{
    /// <summary>
    /// Registers the per-process <see cref="Microsoft.Extensions.Caching.Memory.IMemoryCache"/>
    /// and in-flight request coalescer used by <see cref="Handlers.GetThirdPartyStringWithRetryHandler"/>.
    /// </summary>
    public static IServiceCollection AddThirdPartyHttp(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddSingleton<ThirdPartyRequestCoalescer>();
        return services;
    }
}
