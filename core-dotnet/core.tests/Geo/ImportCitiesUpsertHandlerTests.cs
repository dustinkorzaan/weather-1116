using Core.Data;
using Core.Data.Domain;
using Core.Geo.Events;
using Core.Geo.Handlers;
using Core.Geo.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Core.Tests.Geo;

public class ImportCitiesUpsertHandlerTests
{
    [Fact]
    public void ApplyChanges_NewCity_SetsEveryFieldAndGeoPoint()
    {
        var city = new City();

        Assert.True(ImportCitiesUpsertHandler.ApplyChanges(city, Nashville()));

        Assert.Equal("Nashville", city.Name);
        Assert.Equal("Tennessee", city.Admin1Name);
        Assert.Equal(4326, city.GeoPoint.SRID);
        Assert.Equal(-86.78444, city.GeoPoint.X);
        Assert.Equal(36.16589, city.GeoPoint.Y);
    }

    [Fact]
    public void ApplyChanges_IdenticalInput_ReturnsFalseAndKeepsGeoPoint()
    {
        var city = new City();
        ImportCitiesUpsertHandler.ApplyChanges(city, Nashville());
        var point = city.GeoPoint;

        Assert.False(ImportCitiesUpsertHandler.ApplyChanges(city, Nashville()));
        Assert.Same(point, city.GeoPoint);
    }

    [Fact]
    public void ApplyChanges_PopulationChange_KeepsGeoPoint()
    {
        var city = new City();
        ImportCitiesUpsertHandler.ApplyChanges(city, Nashville());
        var point = city.GeoPoint;
        var dto = Nashville();
        dto.Population = 720000;

        Assert.True(ImportCitiesUpsertHandler.ApplyChanges(city, dto));
        Assert.Equal(720000, city.Population);
        Assert.Same(point, city.GeoPoint);
    }

    [Fact]
    public void ApplyChanges_LatLongChange_RebuildsGeoPoint()
    {
        var city = new City();
        ImportCitiesUpsertHandler.ApplyChanges(city, Nashville());
        var point = city.GeoPoint;
        var dto = Nashville();
        dto.Latitude = 36.2;

        Assert.True(ImportCitiesUpsertHandler.ApplyChanges(city, dto));
        Assert.NotSame(point, city.GeoPoint);
        Assert.Equal(36.2, city.GeoPoint.Y);
        Assert.Equal(36.2, city.Latitude);
    }

    [Fact]
    public async Task Handle_InsertsUpdatesAndCountsUnchanged_ThenSavesNothingOnARepeat()
    {
        using var db = CreateDb();
        var existing = new City { Id = Guid.NewGuid(), GeonameId = 4644585 };
        ImportCitiesUpsertHandler.ApplyChanges(existing, Nashville());
        var stale = new City { Id = Guid.NewGuid(), GeonameId = 3041563 };
        var andorra = Andorra();
        ImportCitiesUpsertHandler.ApplyChanges(stale, andorra);
        stale.Population = 1;
        db.Cities.AddRange(existing, stale);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var upsert = new ImportCitiesUpsertEvent { Cities = [Nashville(), andorra, Manhattan()] };
        var handler = new ImportCitiesUpsertHandler(db, NullLogger<ImportCitiesUpsertHandler>.Instance);

        var first = await handler.Handle(upsert, CancellationToken.None);

        Assert.Equal(1, first.Inserted);
        Assert.Equal(1, first.Updated);
        Assert.Equal(1, first.Unchanged);
        db.ChangeTracker.Clear();
        Assert.Equal(20430, (await db.Cities.SingleAsync(city => city.GeonameId == 3041563)).Population);
        var manhattan = await db.Cities.SingleAsync(city => city.GeonameId == 5125771);
        Assert.NotEqual(Guid.Empty, manhattan.Id);
        Assert.Equal("New York", manhattan.Admin1Name);

        db.ChangeTracker.Clear();
        var second = await handler.Handle(upsert, CancellationToken.None);

        Assert.Equal(0, second.Inserted);
        Assert.Equal(0, second.Updated);
        Assert.Equal(3, second.Unchanged);
        Assert.False(db.ChangeTracker.HasChanges());
    }

    private static GeoNamesCityDto Nashville() => new()
    {
        GeonameId = 4644585, Name = "Nashville", CountryCode = "US", Admin1Code = "TN", Admin1Name = "Tennessee",
        FeatureCode = "PPLA", Latitude = 36.16589, Longitude = -86.78444, Population = 715884, Timezone = "America/Chicago",
    };

    private static GeoNamesCityDto Manhattan() => new()
    {
        GeonameId = 5125771, Name = "Manhattan", CountryCode = "US", Admin1Code = "NY", Admin1Name = "New York",
        FeatureCode = "PPLX", Latitude = 40.78343, Longitude = -73.96625, Population = 1694251, Timezone = "America/New_York",
    };

    private static GeoNamesCityDto Andorra() => new()
    {
        GeonameId = 3041563, Name = "Andorra la Vella", CountryCode = "AD", Admin1Code = "07", Admin1Name = "Andorra la Vella",
        FeatureCode = "PPLC", Latitude = 42.50779, Longitude = 1.52109, Population = 20430, Timezone = "Europe/Andorra",
    };

    private static WX1116DbContext CreateDb() =>
        new(new DbContextOptionsBuilder<WX1116DbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
