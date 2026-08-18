using Core.Weather.Models;
using MediatR;
using System.Text.Json.Serialization;

namespace Core.Weather.Events;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PublicWeatherHistoryResolution
{
    Daily,
    Hourly,
}

/// <summary>
/// Fetches recent past public weather for a latitude/longitude via Open-Meteo.
/// </summary>
public class GetPublicWeatherHistoryEvent : IRequest<NonAIHistoryWeatherResponse>
{
    public required double Latitude { get; set; }

    public required double Longitude { get; set; }

    public PublicWeatherHistoryResolution Resolution { get; set; } = PublicWeatherHistoryResolution.Daily;
}
