using System.Reflection;

namespace MediatR;

public sealed class MediatRServiceConfiguration
{
    private readonly List<Assembly> _assemblies = [];

    public MediatRServiceConfiguration RegisterServicesFromAssemblyContaining<T>()
        => RegisterServicesFromAssembly(typeof(T).Assembly);

    public MediatRServiceConfiguration RegisterServicesFromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        if (!_assemblies.Contains(assembly))
        {
            _assemblies.Add(assembly);
        }

        return this;
    }

    internal IReadOnlyList<Assembly> Assemblies => _assemblies;
}
