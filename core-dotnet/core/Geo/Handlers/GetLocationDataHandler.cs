using System.Globalization;
using System.Text.Json;
using Core.Geo.Events;
using Core.Geo.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Core.Geo.Handlers;

/// <summary>
/// Reverse-geocodes lat/long to a City, State (or City, State, Country) label using Nominatim.
/// </summary>
public class GetLocationDataHandler : IRequestHandler<GetLocationDataEvent, NominatimLocationResponse>
{
    internal const string UserAgent = "Weather-1116/1.0 (https://github.com/dustinkorzaan/weather-1116)";

    private readonly ILogger<GetLocationDataHandler> _logger;

    public GetLocationDataHandler(ILogger<GetLocationDataHandler> logger)
    {
        _logger = logger;
    }

    public async Task<NominatimLocationResponse> Handle(GetLocationDataEvent request, CancellationToken cancellationToken)
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", UserAgent);
        client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");

        var url = string.Create(
            CultureInfo.InvariantCulture,
            $"https://nominatim.openstreetmap.org/reverse?lat={request.Latitude}&lon={request.Longitude}&format=jsonv2&addressdetails=1&zoom=10&accept-language=en");

        string jsonResponse = await client.GetStringAsync(url, cancellationToken);
        var geoData = JsonSerializer.Deserialize<NominatimReverseResponse>(jsonResponse);
        var location = NominatimLocationMapper.FromAddress(geoData?.Address);

        if (string.IsNullOrWhiteSpace(location))
        {
            throw new InvalidOperationException(
                $"Nominatim: No location found for {request.Latitude.ToString(CultureInfo.InvariantCulture)}, {request.Longitude.ToString(CultureInfo.InvariantCulture)}.");
        }

        _logger.LogInformation(
            "Nominatim: Reverse geocoded {Latitude}, {Longitude} to {Location}",
            request.Latitude,
            request.Longitude,
            location);

        return new NominatimLocationResponse { Location = location };
    }
}
