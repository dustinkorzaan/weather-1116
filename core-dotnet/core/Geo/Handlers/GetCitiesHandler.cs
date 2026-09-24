using System.Text.Json;
using Core.Caching;
using Core.Data;
using Core.Geo.Events;
using Core.Geo.Models;
using CQMediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Geo.Handlers;

/// <summary>
/// Finds the largest cities within a radius of a lat/long by querying dbo.City (GeoNames cities500,
/// loaded daily by <see cref="ImportCitiesHandler"/>) with a NetTopologySuite geography distance filter.
/// </summary>
public class GetCitiesHandler : IRequestHandler<GetCitiesEvent, NonAICitiesResponse>
{
    // GeoNames feature codes for city sections (e.g. Manhattan inside New York City) and
    // historical, abandoned, or destroyed places. Imported, but not returned for now.
    internal static readonly string[] ExcludedFeatureCodes = ["PPLX", "PPLH", "PPLQ", "PPLW"];

    private readonly CacheHelper _cache;
    private readonly WX1116DbContext _db;
    private readonly ILogger<GetCitiesHandler> _logger;

    public GetCitiesHandler(
        CacheHelper cache,
        WX1116DbContext db,
        ILogger<GetCitiesHandler> logger)
    {
        _cache = cache;
        _db = db;
        _logger = logger;
    }

    public async Task<NonAICitiesResponse> Handle(GetCitiesEvent request, CancellationToken cancellationToken)
    {
        request.RadiusKm = GetCitiesEvent.NormalizeRadiusKm(request.RadiusKm);
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
        var origin = ImportCitiesHandler.CreatePoint(request.Latitude, request.Longitude);
        var radiusMeters = request.RadiusKm * 1000;
        var minPopulation = request.MinPopulation;

        var query = _db.City
            .AsNoTracking()
            .Where(city => city.GeoPoint.IsWithinDistance(origin, radiusMeters)
                && city.Population >= minPopulation
                && !ExcludedFeatureCodes.Contains(city.FeatureCode));

        var response = NewResponse(request);
        response.TotalAvailable = await query.CountAsync(cancellationToken);
        response.Cities = await query
            .OrderByDescending(city => city.Population)
            .Take(request.MaxCities)
            .Select(city => new NonAICity
            {
                Name = city.Name,
                Region = city.Admin1Name,
                Country = city.CountryCode,
                Latitude = city.Latitude,
                Longitude = city.Longitude,
                DistanceKm = city.GeoPoint.Distance(origin) / 1000,
                Population = city.Population,
            })
            .ToListAsync(cancellationToken);
        response.Returned = response.Cities.Count;

        _logger.LogInformation(
            "GetCities: {Returned} of {TotalAvailable} cities within {RadiusKm} km",
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
}
