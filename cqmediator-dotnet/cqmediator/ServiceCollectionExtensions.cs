using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCQMediator(
        this IServiceCollection services,
        Action<CQMediator.CQMediatorServiceConfiguration> configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var cqMediatorConfiguration = new CQMediator.CQMediatorServiceConfiguration();
        configuration(cqMediatorConfiguration);

        // Transient so Mediator resolves handlers from the current request/scope,
        // matching the NuGet MediatR lifetime and avoiding root-provider capture.
        services.AddTransient<CQMediator.IMediator, CQMediator.Mediator>();

        foreach (var assembly in cqMediatorConfiguration.Assemblies)
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
                if (genericDefinition == typeof(CQMediator.IRequestHandler<,>)
                    || genericDefinition == typeof(CQMediator.IRequestHandler<>))
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
