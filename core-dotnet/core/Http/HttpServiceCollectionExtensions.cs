using Core.Http.Events;
using Core.Http.Handlers;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Http;

public static class HttpServiceCollectionExtensions
{
    /// <summary>
    /// Registers the <see cref="HttpClient"/> and <see cref="Microsoft.Extensions.Caching.Memory.IMemoryCache"/>
    /// used by <see cref="GetCachedThirdPartyStringWithRetryHandler"/>.
    /// </summary>
    public static IServiceCollection AddThirdPartyHttp(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddHttpClient<GetCachedThirdPartyStringWithRetryHandler>();
        services.AddTransient<IRequestHandler<GetCachedThirdPartyStringWithRetryEvent, string>>(
            static sp => sp.GetRequiredService<GetCachedThirdPartyStringWithRetryHandler>());
        return services;
    }
}
