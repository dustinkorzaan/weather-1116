using Core.Data;
using Core.Data.Domain;
using Core.Geo.Events;
using Core.Geo.Handlers;
using Core.Geo.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Core.Tests.Geo;

public class ImportCitiesDeleteHandlerTests
{
    [Fact]
    public void MissingIdBatches_SkipsImportedIdsAndSplitsTheRest()
    {
        var existing = Enumerable.Range(1, 3_000);
        var imported = Enumerable.Range(1, 500).ToHashSet();

        var batches = ImportCitiesDeleteHandler.MissingIdBatches(existing, imported).ToList();

        Assert.Equal([1_000, 1_000, 500], batches.Select(batch => batch.Length));
        Assert.Equal(501, batches[0][0]);
        Assert.Equal(3_000, batches[^1][^1]);
    }

    [Fact]
    public void ParseIds_ReadsOneIdPerLine()
    {
        Assert.Equal([4644585, 3041563, 5125771], ImportCitiesDeleteHandler.ParseIds("4644585\n3041563\r\n5125771\n"));
    }

    // The delete itself (ExecuteDelete) needs a relational provider; CitySqlServerTests covers it.
    [Fact]
    public async Task Handle_NothingMissing_DeletesNoRowsAndRemovesTheIdsFile()
    {
        using var db = CreateDb();
        db.Cities.AddRange(Enumerable.Range(1, 3).Select(NewCity));
        await db.SaveChangesAsync();
        var blobs = new FakeCityImportBlobStore();
        blobs.Blobs["geonameids.txt"] = "1\n2\n3\n4";
        var handler = new ImportCitiesDeleteHandler(db, NullLogger<ImportCitiesDeleteHandler>.Instance, blobs);

        var response = await handler.Handle(new ImportCitiesDeleteEvent { GeonameIdsBlob = "geonameids.txt" }, CancellationToken.None);

        Assert.Equal(0, response.Deleted);
        Assert.Equal(3, await db.Cities.CountAsync());
        Assert.False(blobs.Blobs.ContainsKey("geonameids.txt"));
    }

    private static City NewCity(int geonameId)
    {
        var city = new City { Id = Guid.NewGuid(), GeonameId = geonameId };
        ImportCitiesUpsertHandler.ApplyChanges(city, new GeoNamesCityDto { GeonameId = geonameId, Name = $"City {geonameId}" });
        return city;
    }

    private static WX1116DbContext CreateDb() =>
        new(new DbContextOptionsBuilder<WX1116DbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
