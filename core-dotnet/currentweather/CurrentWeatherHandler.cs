using MediatR;

namespace Core.currentweather;

/// <summary>
/// Handles <see cref="CurrentWeatherEvent"/> by delegating to the registered
/// <see cref="ICurrentWeatherSource"/> for the actual data fetch.
/// </summary>
public class CurrentWeatherHandler : IRequestHandler<CurrentWeatherEvent, CurrentWeatherConditions>
{
    private readonly ICurrentWeatherSource _source;

    public CurrentWeatherHandler(ICurrentWeatherSource source)
    {
        _source = source;
    }

    public Task<CurrentWeatherConditions> Handle(CurrentWeatherEvent request, CancellationToken cancellationToken)
    {
        return _source.GetCurrentWeatherAsync(request.Location, cancellationToken);
    }
}
