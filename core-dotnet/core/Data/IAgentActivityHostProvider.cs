namespace Core.Data;

/// <summary>
/// Tells <see cref="AgentActivityLogger"/> which host it's running in. API and MVC each
/// register this once in Program.cs with their own literal value (<see cref="Domain.AgentActivityHost"/>) --
/// Core itself has no notion of which host loaded it.
/// </summary>
public interface IAgentActivityHostProvider
{
    string Host { get; }
}

public sealed class AgentActivityHostProvider(string host) : IAgentActivityHostProvider
{
    public string Host { get; } = host;
}
