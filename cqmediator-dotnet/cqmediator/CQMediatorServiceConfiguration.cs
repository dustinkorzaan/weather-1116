using System.Reflection;

namespace CQMediator;

public sealed class CQMediatorServiceConfiguration
{
    private readonly List<Assembly> _assemblies = [];

    public CQMediatorServiceConfiguration RegisterServicesFromAssemblyContaining<T>()
        => RegisterServicesFromAssembly(typeof(T).Assembly);

    public CQMediatorServiceConfiguration RegisterServicesFromAssembly(Assembly assembly)
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
