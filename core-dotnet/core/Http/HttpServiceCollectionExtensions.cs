using Core.Http.Events;
using Core.Http.Handlers;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Http;

public static class HttpServiceCollectionExtensions
{
    /// <summary>
    /// Registers the per-process cache, in-flight coalescer, and <see cref="HttpClient"/>
    /// used by <see cref="GetThirdPartyStringWithRetryHandler"/>.
    /// </summary>
    public static IServiceCollection AddThirdPartyHttp(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddSingleton<ThirdPartyRequestCoalescer>();
        services.AddHttpClient<GetThirdPartyStringWithRetryHandler>();
        services.AddTransient<IRequestHandler<GetThirdPartyStringWithRetryEvent, string>>(
            static sp => sp.GetRequiredService<GetThirdPartyStringWithRetryHandler>());
        return services;
    }
}
