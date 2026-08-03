using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMediatR(
        this IServiceCollection services,
        Action<MediatR.MediatRServiceConfiguration> configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var mediatRConfiguration = new MediatR.MediatRServiceConfiguration();
        configuration(mediatRConfiguration);

        // Transient so Mediator resolves handlers from the current request/scope,
        // matching the NuGet MediatR lifetime and avoiding root-provider capture.
        services.AddTransient<MediatR.IMediator, MediatR.Mediator>();

        foreach (var assembly in mediatRConfiguration.Assemblies)
        {
            RegisterHandlersFromAssembly(services, assembly);
        }

        return services;
    }

    private static void RegisterHandlersFromAssembly(IServiceCollection services, Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            if (type.IsAbstract || type.IsInterface)
            {
                continue;
            }

            foreach (var serviceType in type.GetInterfaces())
            {
                if (!serviceType.IsGenericType)
                {
                    continue;
                }

                var genericDefinition = serviceType.GetGenericTypeDefinition();
                if (genericDefinition == typeof(MediatR.IRequestHandler<,>)
                    || genericDefinition == typeof(MediatR.IRequestHandler<>))
                {
                    services.AddTransient(serviceType, type);
                }
            }
        }
    }
}
