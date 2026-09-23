using System.Globalization;
using System.Net;
using System.Text.Json;
using Core.Caching;
using Core.Geo.Events;
using Core.Geo.Models;
using Core.Http;
using CQMediator;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Core.Geo.Handlers;

/// <summary>
/// Finds the largest cities within a radius of a lat/long via GeoDB Cities' free (no-key) service,
/// paging until <see cref="GetCitiesEvent.MaxCities"/> cities are collected or GeoDB runs out.
/// </summary>
public class GetCitiesHandler : IRequestHandler<GetCitiesEvent, NonAICitiesResponse>
{
    internal const string GeoDbBaseUrl = "https://geodb-free-service.wirefreethought.com/v1/geo";

    // GeoDB's free tier returns at most 10 results per request, allows about 1 request per second,
    // and rejects (403) any radius above 100 of the requested unit.
    internal const int GeoDbPageLimit = 10;
    internal const double GeoDbMaxRadiusKm = 100;

    private readonly CacheHelper _cache;
    private readonly TransientRetryHelper _retry;
    private readonly IHttpClientFactory _clientFactory;
    private readonly ILogger<GetCitiesHandler> _logger;

    public GetCitiesHandler(
        CacheHelper cache,
        TransientRetryHelper retry,
        IHttpClientFactory clientFactory,
        ILogger<GetCitiesHandler> logger)
    {
        _cache = cache;
        _retry = retry;
        _clientFactory = clientFactory;
        _logger = logger;
    }

    internal TimeSpan PageDelay { get; init; } = TimeSpan.FromMilliseconds(1100);

    public async Task<NonAICitiesResponse> Handle(GetCitiesEvent request, CancellationToken cancellationToken)
    {
        request.RadiusKm = Math.Min(GetCitiesEvent.NormalizeRadiusKm(request.RadiusKm), GeoDbMaxRadiusKm);
        request.MinPopulation = GetCitiesEvent.NormalizeMinPopulation(request.MinPopulation);
        request.MaxCities = GetCitiesEvent.NormalizeMaxCities(request.MaxCities);

        if (request.MaxCities == 0)
        {
            return NewResponse(request);
        }

        var cacheKey = JsonSerializer.Serialize(new { Handler = nameof(GetCitiesHandler), Request = request });
        return await _cache.GetOrCreate(
            cacheKey: cacheKey,
            cacheDuration: TimeSpan.FromMinutes(60),
            valueFactory: ct => GetCities(request, ct),
            cancellationToken: cancellationToken);
    }

    private async Task<NonAICitiesResponse> GetCities(GetCitiesEvent request, CancellationToken cancellationToken)
    {
        using var client = _clientFactory.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", GetLocationHandler.UserAgent);
        client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");

        var response = NewResponse(request);
        var offset = 0;

        while (response.Cities.Count < request.MaxCities)
        {
            if (offset > 0)
            {
                await Task.Delay(PageDelay, cancellationToken);
            }

            var limit = Math.Min(GeoDbPageLimit, request.MaxCities - response.Cities.Count);
            var url = BuildNearbyCitiesUrl(request.Latitude, request.Longitude, request.RadiusKm, request.MinPopulation, limit, offset);

            // codeql[cs/exposure-of-sensitive-information]
            var page = await _retry.Execute(async ct =>
            {
                using var httpResponse = await client.GetAsync(url, ct);
                if (IsPermanentFailure(httpResponse.StatusCode))
                {
                    // Not an HttpRequestException, so TransientRetryHelper does not retry it.
                    throw new InvalidOperationException($"GeoDB rejected the request with HTTP {(int)httpResponse.StatusCode}.");
                }

                httpResponse.EnsureSuccessStatusCode();
                var json = await httpResponse.Content.ReadAsStringAsync(ct);
                return JsonSerializer.Deserialize<GeoDbNearbyCitiesResponse>(json) ?? new GeoDbNearbyCitiesResponse();
            }, cancellationToken);

            var data = page.Data ?? [];
            response.TotalAvailable = page.Metadata?.TotalCount ?? response.TotalAvailable;
            response.Cities.AddRange(data.Select(ToCity));
            offset += data.Count;

            if (data.Count == 0 || offset >= response.TotalAvailable)
            {
                break;
            }
        }

        response.Returned = response.Cities.Count;
        _logger.LogInformation(
            "GeoDB: {Returned} of {TotalAvailable} cities within {RadiusKm} km",
            response.Returned,
            response.TotalAvailable,
            response.RadiusKm);

        return response;
    }

    private static NonAICitiesResponse NewResponse(GetCitiesEvent request) => new()
    {
        RadiusKm = request.RadiusKm,
        MinPopulation = request.MinPopulation,
        MaxCities = request.MaxCities,
    };

    internal static bool IsPermanentFailure(HttpStatusCode statusCode) =>
        (int)statusCode is >= 400 and < 500 && statusCode != HttpStatusCode.TooManyRequests;

    internal static NonAICity ToCity(GeoDbCity city) => new()
    {
        Name = city.Name ?? city.City ?? string.Empty,
        Region = city.Region,
        Country = city.Country,
        Latitude = city.Latitude,
        Longitude = city.Longitude,
        DistanceKm = city.Distance ?? 0,
        Population = city.Population,
    };

    // GeoDB location ids are ISO-6709 (e.g. +36.1627-086.7816); the leading '+' must be escaped.
    internal static string BuildNearbyCitiesUrl(double latitude, double longitude, double radiusKm, long minPopulation, int limit, int offset)
    {
        var locationId = string.Create(CultureInfo.InvariantCulture, $"{latitude:+00.0000;-00.0000}{longitude:+000.0000;-000.0000}");
        var populationFilter = minPopulation > 0
            ? string.Create(CultureInfo.InvariantCulture, $"&minPopulation={minPopulation}")
            : string.Empty;
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{GeoDbBaseUrl}/locations/{Uri.EscapeDataString(locationId)}/nearbyCities?radius={radiusKm:0.###}&distanceUnit=KM&types=CITY{populationFilter}&sort=-population&limit={limit}&offset={offset}");
    }
}
