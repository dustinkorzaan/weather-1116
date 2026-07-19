using System.Text.Json;
using Core.geo.Events;
using Core.geo.Models;
using MediatR;

namespace Core.geo.Handlers;

/// <summary>
/// Geocodes a location string to latitude/longitude using Open-Meteo.
/// </summary>
public class GetLatLongDataHandler : IRequestHandler<GetLatLongDataEvent, NonAILatLongResponse>
{
    public async Task<NonAILatLongResponse> Handle(GetLatLongDataEvent request, CancellationToken cancellationToken)
    {
        var client = new HttpClient();

        try
        {
            // Try multiple location variants to handle inputs like "City, ST".
            var queries = new List<string> { request.Location };
            if (request.Location.Contains(','))
            {
                queries.Add(request.Location.Split(',')[0].Trim());
            }

            foreach (var query in queries.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                string encodedLocation = Uri.EscapeDataString(query);
                string url = $"https://geocoding-api.open-meteo.com/v1/search?name={encodedLocation}&count=1&language=en&format=json";
                Console.WriteLine($"Non-AI: Fetching geocoding data from: {url}");
                string jsonResponse = await client.GetStringAsync(url, cancellationToken);
                var geoData = JsonSerializer.Deserialize<NonAIGeocodingResponse>(jsonResponse);

                if (geoData?.Results != null && geoData.Results.Count > 0)
                {
                    var topMatch = geoData.Results[0];
                    Console.WriteLine($"Non-AI: Found: {topMatch.Name}, {topMatch.Admin1}, {topMatch.Country}");
                    return new NonAILatLongResponse { Latitude = topMatch.Latitude, Longitude = topMatch.Longitude };
                }
            }

            Console.WriteLine($"Non-AI: No results found for '{request.Location}'.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Non-AI: An error occurred: {ex.Message}");
        }

        // BNA: 36.1317° N, -86.6688° W
        return new NonAILatLongResponse { Latitude = 36.1317, Longitude = -86.6688 };
    }
}
