using Core.Http;
using Core.Http.Events;
using Core.Http.Handlers;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Tests.Http;

public class HttpServiceCollectionExtensionsTests
{
    [Fact]
    public void AddThirdPartyHttp_ResolvesHandlerWithInjectedHttpClient()
    {
        var services = new ServiceCollection();
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<GetThirdPartyStringWithRetryHandler>());
        services.AddThirdPartyHttp();
        using var provider = services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IRequestHandler<GetThirdPartyStringWithRetryEvent, string>>();

        Assert.IsType<GetThirdPartyStringWithRetryHandler>(handler);
    }
}
