namespace Core.Data;

/// <summary>
/// Tells <see cref="WeatherActivityLogger"/> which host it's running in. API and MVC each
/// register this once in Program.cs with their own literal value (<see cref="Domain.WeatherActivityHost"/>) --
/// Core itself has no notion of which host loaded it.
/// </summary>
public interface IWeatherActivityHostProvider
{
    string Host { get; }
}

public sealed class WeatherActivityHostProvider(string host) : IWeatherActivityHostProvider
{
    public string Host { get; } = host;
}
