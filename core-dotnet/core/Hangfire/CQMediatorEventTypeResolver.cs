using System.Reflection;
using CQMediator;

namespace Core.Hangfire;

internal static class CQMediatorEventTypeResolver
{
    public static Type Resolve(string assemblyQualifiedName)
    {
        var eventType = Type.GetType(assemblyQualifiedName, AssemblyResolver, typeResolver: null)
            ?? throw new InvalidOperationException(
                $"Could not resolve CQMediator event type '{assemblyQualifiedName}'.");

        ValidateIsRequest(eventType);
        return eventType;
    }

    private static Assembly? AssemblyResolver(AssemblyName assemblyName)
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(assembly =>
                string.Equals(assembly.GetName().Name, assemblyName.Name, StringComparison.OrdinalIgnoreCase));
    }

    private static void ValidateIsRequest(Type eventType)
    {
        if (eventType.IsAbstract)
        {
            throw new InvalidOperationException($"'{eventType.Name}' must be a concrete CQMediator request type.");
        }

        if (typeof(IRequest).IsAssignableFrom(eventType))
        {
            return;
        }

        if (eventType.GetInterfaces().Any(
            @interface => @interface.IsGenericType
                && @interface.GetGenericTypeDefinition() == typeof(IRequest<>)))
        {
            return;
        }

        throw new InvalidOperationException($"'{eventType.Name}' is not a CQMediator request type.");
    }
}
