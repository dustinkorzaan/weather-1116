using System.Globalization;
using System.Text.Json;
using Core.Geo;
using Core.Geo.Events;
using Core.Geo.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Core.Geo.Handlers;

/// <summary>
/// Reverse-geocodes lat/long to a place label: City, State (or City, State, Country),
/// then a feature name, then a formatted coordinate.
/// </summary>
public class GetLocationHandler : IRequestHandler<GetLocationEvent, NonAILocationResponse>
{
    internal const string UserAgent = "Weather-1116/1.0 (https://github.com/dustinkorzaan/weather-1116)";

    private readonly ILogger<GetLocationHandler> _logger;

    public GetLocationHandler(ILogger<GetLocationHandler> logger)
    {
        _logger = logger;
    }

    public async Task<NonAILocationResponse> Handle(GetLocationEvent request, CancellationToken cancellationToken)
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", UserAgent);
        client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");

        var url = BuildReverseGeocodeUrl(request.Latitude, request.Longitude);

        // Reverse geocoding has to send coordinates to Nominatim; do not log them.
        // codeql[cs/exposure-of-sensitive-information]
        string jsonResponse = await client.GetStringAsync(url, cancellationToken);
        var geoData = JsonSerializer.Deserialize<NominatimReverseResponse>(jsonResponse);
        var location = NominatimLocationMapper.FromReverse(geoData, request.Latitude, request.Longitude);

        _logger.LogInformation("Nominatim: Reverse geocoded to {Location}", location);

        return new NonAILocationResponse { Location = location };
    }

    internal static string BuildReverseGeocodeUrl(double latitude, double longitude) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"https://nominatim.openstreetmap.org/reverse?lat={latitude}&lon={longitude}&format=jsonv2&addressdetails=1&zoom=10&accept-language=en");
}
