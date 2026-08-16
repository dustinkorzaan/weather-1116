using Core.Weather.Models;
using MediatR;

namespace Core.Weather.Events;

/// <summary>
/// Fetches current weather for a latitude/longitude via the Open-Meteo forecast API.
/// </summary>
public class GetPublicWeatherDataEvent : IRequest<NonAIWeatherResponse>
{
    public required double Latitude { get; set; }

    public required double Longitude { get; set; }
}
