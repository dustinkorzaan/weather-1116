using System.Text.Json;
using Core.Caching;
using Core.Geo.Events;
using Core.Geo.Models;
using Core.Http;
using MediatR;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Core.Geo.Handlers;

/// <summary>
/// Geocodes a location string to ranked latitude/longitude matches using Open-Meteo.
/// </summary>
public class GetLatLongHandler : IRequestHandler<GetLatLongEvent, NonAILatLongListResponse>
{
    private readonly CacheHelper _cache;
    private readonly TransientRetryHelper _retry;
    private readonly IHttpClientFactory _clientFactory;
    private readonly ILogger<GetLatLongHandler> _logger;

    public GetLatLongHandler(
        CacheHelper cache,
        TransientRetryHelper retry,
        IHttpClientFactory clientFactory,
        ILogger<GetLatLongHandler> logger)
    {
        _cache = cache;
        _retry = retry;
        _clientFactory = clientFactory;
        _logger = logger;
    }

    public Task<NonAILatLongListResponse> Handle(GetLatLongEvent request, CancellationToken cancellationToken)
    {
        var cacheKey = JsonSerializer.Serialize(new { Handler = nameof(GetLatLongHandler), Request = request });
        return _cache.GetOrCreateAsync(
            cacheKey,
            TimeSpan.FromMinutes(5),
            ct => _retry.ExecuteAsync(c => GetLatLong(request, c), ct),
            cancellationToken);
    }

    private async Task<NonAILatLongListResponse> GetLatLong(GetLatLongEvent request, CancellationToken cancellationToken)
    {
        using var client = _clientFactory.CreateClient();
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
            string jsonResponse = await client.GetStringAsync(url, cancellationToken);
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
