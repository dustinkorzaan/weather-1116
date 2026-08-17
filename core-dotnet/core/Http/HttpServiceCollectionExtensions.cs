using Microsoft.Extensions.DependencyInjection;

namespace Core.Http;

public static class HttpServiceCollectionExtensions
{
    /// <summary>
    /// Registers the per-process <see cref="Microsoft.Extensions.Caching.Memory.IMemoryCache"/>
    /// used by <see cref="Handlers.GetThirdPartyStringWithRetryHandler"/>.
    /// </summary>
    public static IServiceCollection AddThirdPartyHttp(this IServiceCollection services)
    {
        services.AddMemoryCache();
        return services;
    }
}
