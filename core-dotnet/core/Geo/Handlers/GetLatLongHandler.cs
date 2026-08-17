using System.Text.Json;
using Core.Geo.Events;
using Core.Geo.Models;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Core.Geo.Handlers;

/// <summary>
/// Geocodes a location string to ranked latitude/longitude matches using Open-Meteo.
/// </summary>
public class GetLatLongHandler : IRequestHandler<GetLatLongEvent, NonAILatLongListResponse>
{
    internal const int RetryCount = 5;
    internal const int RetryDelay = 200;

    private readonly IMemoryCache _cache;
    private readonly IHttpClientFactory _clientFactory;
    private readonly ILogger<GetLatLongHandler> _logger;

    public GetLatLongHandler(IMemoryCache cache, IHttpClientFactory clientFactory, ILogger<GetLatLongHandler> logger)
    {
        _cache = cache;
        _clientFactory = clientFactory;
        _logger = logger;
    }

    public async Task<NonAILatLongListResponse> Handle(GetLatLongEvent request, CancellationToken cancellationToken)
    {
        var cacheKey = JsonSerializer.Serialize(new { Handler = nameof(GetLatLongHandler), Request = request });

        if (_cache.TryGetValue(cacheKey, out NonAILatLongListResponse? cached))
        {
            return cached!;
        }

        for (var attempt = 0; ; attempt++)
        {
            try
            {
                var result = await GetLatLong(request, cancellationToken);
                _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
                return result;
            }
            catch when (attempt < RetryCount)
            {
                await Task.Delay((int)(RetryDelay * Math.Pow(2, attempt)), cancellationToken);
            }
        }
    }

    private async Task<NonAILatLongListResponse> GetLatLong(GetLatLongEvent request, CancellationToken cancellationToken)
    {
        using var client = _clientFactory.CreateClient();
        var count = NonAILatLongMapper.NormalizeCount(request.Count);
        var options = new JsonSerializerOptions { WriteIndented = true };

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
            var geoData = JsonSerializer.Deserialize<NonAIGeocodingResponse>(jsonResponse, options);

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
