using System.Text.Json;
using Core.Geo.Events;
using Core.Geo.Models;
using Core.Http;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Core.Geo.Handlers;

/// <summary>
/// Geocodes a location string to ranked latitude/longitude matches using Open-Meteo.
/// </summary>
public class GetLatLongHandler : IRequestHandler<GetLatLongEvent, NonAILatLongListResponse>
{
    private readonly ILogger<GetLatLongHandler> _logger;

    public GetLatLongHandler(ILogger<GetLatLongHandler> logger)
    {
        _logger = logger;
    }

    public async Task<NonAILatLongListResponse> Handle(GetLatLongEvent request, CancellationToken cancellationToken)
    {
        using var client = new HttpClient();
        var count = NonAILatLongMapper.NormalizeCount(request.Count);

        // Try multiple location variants to handle inputs like "City, ST".
        var queries = new List<string> { request.Location };
        if (request.Location.Contains(','))
        {
            queries.Add(request.Location.Split(',')[0].Trim());
        }

        foreach (var query in queries.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            string encodedLocation = Uri.EscapeDataString(query);
            string url = $"https://geocoding-api.open-meteo.com/v1/search?name={encodedLocation}&count={count}&language=en&format=json";
            string jsonResponse = await ThirdPartyHttp.GetStringWithRetryAsync(client, url, cancellationToken);
            var geoData = JsonSerializer.Deserialize<NonAIGeocodingResponse>(jsonResponse);

            if (geoData?.Results != null && geoData.Results.Count > 0)
            {
                var mapped = NonAILatLongMapper.FromGeocodingResults(geoData.Results.Take(count).ToList());
                var topMatch = mapped.Results[0];
                _logger.LogInformation(
                    "Non-AI: Found {ResultCount} match(es); top: {Name}, {State}, {Country}",
                    mapped.Results.Count,
                    topMatch.Name,
                    topMatch.State,
                    topMatch.Country);
                return mapped;
            }
        }

        throw new InvalidOperationException($"Non-AI: No results found for '{request.Location}'.");
    }
}
