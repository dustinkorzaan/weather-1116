namespace Core.currentweather;

/// <summary>
/// Abstracts the external data source for current weather conditions, enabling
/// the handler to be tested without live HTTP calls.
/// </summary>
public interface ICurrentWeatherSource
{
    Task<CurrentWeatherConditions> GetCurrentWeatherAsync(string location, CancellationToken cancellationToken);
}
