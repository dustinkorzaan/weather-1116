using Core.Data;
using Core.Data.Domain;
using Core.Geo.Events;
using Core.Geo.Models;
using Core.Geo.Services;
using CQMediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;

namespace Core.Geo.Handlers;

/// <summary>
/// Upserts one batch of cities enqueued by <see cref="ImportCitiesHandler"/>: reads the batch's raw
/// cities500 lines and the run's admin1 names from blob storage, does one lookup by GeonameId, then a
/// single SaveChanges only when something in the batch changed. The batch file is deleted once that
/// commits, so a retry after a failure still finds it.
/// </summary>
public class ImportCitiesUpsertHandler : IRequestHandler<ImportCitiesUpsertEvent, ImportCitiesUpsertResponse>
{
    internal const int Srid = 4326;

    private readonly WX1116DbContext _db;
    private readonly ICityImportBlobStore? _blobs;
    private readonly ILogger<ImportCitiesUpsertHandler> _logger;

    // blobs is optional for the same reason as on ImportCitiesHandler: only the worker runs this.
    public ImportCitiesUpsertHandler(
        WX1116DbContext db,
        ILogger<ImportCitiesUpsertHandler> logger,
        ICityImportBlobStore? blobs = null)
    {
        _db = db;
        _blobs = blobs;
        _logger = logger;
    }

    public async Task<ImportCitiesUpsertResponse> Handle(ImportCitiesUpsertEvent request, CancellationToken cancellationToken)
    {
        var blobs = ImportCitiesHandler.RequireBlobs(_blobs);
        var admin1Names = ImportCitiesHandler.ParseAdmin1Names(
            new StringReader(await blobs.DownloadTextAsync(request.Admin1Blob, cancellationToken)));
        var cities = ImportCitiesHandler.Parse(
            new StringReader(await blobs.DownloadTextAsync(request.CitiesBlob, cancellationToken))).ToList();
        foreach (var dto in cities)
        {
            dto.Admin1Name = admin1Names.GetValueOrDefault(ImportCitiesHandler.Admin1Key(dto.CountryCode, dto.Admin1Code));
        }

        var ids = cities.Select(dto => dto.GeonameId).ToList();
        var existing = await _db.Cities
            .Where(city => ids.Contains(city.GeonameId))
            .ToDictionaryAsync(city => city.GeonameId, cancellationToken);

        var response = new ImportCitiesUpsertResponse();
        foreach (var dto in cities)
        {
            if (!existing.TryGetValue(dto.GeonameId, out var city))
            {
                city = new City { Id = Guid.NewGuid(), GeonameId = dto.GeonameId };
                ApplyChanges(city, dto);
                _db.Cities.Add(city);
                response.Inserted++;
            }
            else if (ApplyChanges(city, dto))
            {
                response.Updated++;
            }
            else
            {
                response.Unchanged++;
            }
        }

        if (_db.ChangeTracker.HasChanges())
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        await blobs.DeleteAsync(request.CitiesBlob, cancellationToken);

        _logger.LogInformation(
            "ImportCitiesUpsert: {Inserted} inserted, {Updated} updated, {Unchanged} unchanged",
            response.Inserted,
            response.Updated,
            response.Unchanged);

        return response;
    }

    /// <summary>
    /// Copies <paramref name="dto"/> onto <paramref name="city"/>, assigning only fields whose value
    /// differs so EF change tracking marks just the columns that really changed. Returns whether
    /// anything changed.
    /// </summary>
    internal static bool ApplyChanges(City city, GeoNamesCityDto dto)
    {
        var changed = false;

        if (city.Name != dto.Name) { city.Name = dto.Name; changed = true; }
        if (city.CountryCode != dto.CountryCode) { city.CountryCode = dto.CountryCode; changed = true; }
        if (city.Admin1Code != dto.Admin1Code) { city.Admin1Code = dto.Admin1Code; changed = true; }
        if (city.Admin1Name != dto.Admin1Name) { city.Admin1Name = dto.Admin1Name; changed = true; }
        if (city.FeatureCode != dto.FeatureCode) { city.FeatureCode = dto.FeatureCode; changed = true; }
        if (city.Population != dto.Population) { city.Population = dto.Population; changed = true; }
        if (city.Timezone != dto.Timezone) { city.Timezone = dto.Timezone; changed = true; }

        if (city.GeoPoint is null || city.Latitude != dto.Latitude || city.Longitude != dto.Longitude)
        {
            city.Latitude = dto.Latitude;
            city.Longitude = dto.Longitude;
            city.GeoPoint = CreatePoint(dto.Latitude, dto.Longitude);
            changed = true;
        }

        return changed;
    }

    internal static Point CreatePoint(double latitude, double longitude) =>
        new(longitude, latitude) { SRID = Srid };
}
