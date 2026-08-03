using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
        foreach (var type in GetLoadableTypes(assembly))
        {
            if (type.IsAbstract || type.IsInterface || type.IsGenericTypeDefinition)
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
                    services.TryAddTransient(serviceType, type);
                }
            }
        }
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.OfType<Type>();
        }
    }
}
