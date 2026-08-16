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

        var url = BuildReverseGeocodeUrl(request.Latitude, request.Longitude);

        // Reverse geocoding has to send coordinates to Nominatim; do not log them.
        // codeql[cs/exposure-of-sensitive-information]
        string jsonResponse = await client.GetStringAsync(url, cancellationToken);
        var geoData = JsonSerializer.Deserialize<NominatimReverseResponse>(jsonResponse);
        var location = NominatimLocationMapper.FromAddress(geoData?.Address);

        if (string.IsNullOrWhiteSpace(location))
        {
            throw new InvalidOperationException("Nominatim: No location found for the given coordinates.");
        }

        _logger.LogInformation("Nominatim: Reverse geocoded to {Location}", location);

        return new NominatimLocationResponse { Location = location };
    }

    internal static string BuildReverseGeocodeUrl(double latitude, double longitude) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"https://nominatim.openstreetmap.org/reverse?lat={latitude}&lon={longitude}&format=jsonv2&addressdetails=1&zoom=10&accept-language=en");
}
