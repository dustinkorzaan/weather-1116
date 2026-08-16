using Core.Weather.Models;
using MediatR;
using System.Text.Json.Serialization;

namespace Core.Weather.Events;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PublicWeatherForecastResolution
{
    Daily,
    Hourly,
    FifteenMinutes,
}

/// <summary>
/// Fetches an upcoming public weather forecast for a latitude/longitude via Open-Meteo.
/// </summary>
public class GetPublicWeatherForecastEvent : IRequest<PublicWeatherForecastResponse>
{
    public required double Latitude { get; set; }

    public required double Longitude { get; set; }

    public PublicWeatherForecastResolution Resolution { get; set; } = PublicWeatherForecastResolution.Daily;
}
