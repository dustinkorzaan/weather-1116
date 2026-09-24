using System.IO.Compression;
using System.Net;
using System.Text;
using Core.Caching;
using Core.Data;
using Core.Geo.Events;
using Core.Geo.Handlers;
using Core.Geo.Models;
using Core.Http;
using Core.Tests.Data;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Core.Tests.Geo;

/// <summary>
/// ImportCitiesUpsertHandler, ImportCitiesHandler's bulk delete and GetCitiesHandler against a real,
/// freshly migrated SQL Server database (geography distances in meters, the IX_Cities_GeoPoint
/// spatial index, ExecuteDelete).
/// </summary>
public class CitySqlServerTests : IAsyncLifetime
{
    // Declared before Cities, whose initializer reads it.
    private static readonly Dictionary<string, string> Admin1Names = new()
    {
        ["US.TN"] = "Tennessee",
        ["US.GA"] = "Georgia",
    };

    private static readonly GeoNamesCityDto[] Cities =
    [
        City(4644585, "Nashville", "TN", "PPLA", 36.16589, -86.78444, 715884),
        City(4641239, "Memphis", "TN", "PPLA2", 35.14953, -90.04898, 633104),
        City(4645421, "Murfreesboro", "TN", "PPLA2", 35.84562, -86.39027, 152769),
        City(4623560, "Franklin", "TN", "PPLA2", 35.92506, -86.86889, 83454),
        City(4180439, "Atlanta", "GA", "PPLA", 33.749, -84.38798, 498715),
        // A city section: imported, but GetCities must never return it.
        City(9990001, "East Nashville", "TN", "PPLX", 36.1767, -86.7486, 900000),
        City(2643743, "London", "ENG", "PPLC", 51.50853, -0.12574, 8961989),
    ];

    private readonly string _connectionString = BuildConnectionString();

    public async Task InitializeAsync()
    {
        if (SqlServerFactAttribute.ConnectionString is null)
        {
            return;
        }

        await using var db = CreateDb();
        await db.Database.MigrateAsync();
        await Upsert(db, Cities);
    }

    public async Task DisposeAsync()
    {
        if (SqlServerFactAttribute.ConnectionString is null)
        {
            return;
        }

        await using var db = CreateDb();
        await db.Database.EnsureDeletedAsync();
    }

    [SqlServerFact]
    public async Task GetCities_ReturnsLargestFirstWithinRadius_SkippingCitySections()
    {
        var response = await GetCities(new GetCitiesEvent { Latitude = 36.16589, Longitude = -86.78444, RadiusKm = 100 });

        Assert.Equal(["Nashville", "Murfreesboro", "Franklin"], response.Cities.Select(city => city.Name));
        Assert.Equal(3, response.TotalAvailable);
        Assert.Equal(3, response.Returned);

        var murfreesboro = response.Cities[1];
        Assert.Equal("Tennessee", murfreesboro.Region);
        Assert.Equal("US", murfreesboro.Country);
        Assert.Equal(152769, murfreesboro.Population);
        Assert.InRange(murfreesboro.DistanceKm, 45, 57);
        Assert.Equal(0, response.Cities[0].DistanceKm, 3);
    }

    [SqlServerFact]
    public async Task GetCities_AppliesMinPopulationBeyondTheOld100KmLimit()
    {
        var response = await GetCities(new GetCitiesEvent
        {
            Latitude = 36.16589,
            Longitude = -86.78444,
            RadiusKm = 400,
            MinPopulation = 500000,
        });

        Assert.Equal(400, response.RadiusKm);
        Assert.Equal(["Nashville", "Memphis"], response.Cities.Select(city => city.Name));
        Assert.InRange(response.Cities[1].DistanceKm, 300, 340);
    }

    [SqlServerFact]
    public async Task GetCities_TakesMaxCitiesButCountsEveryMatch()
    {
        var response = await GetCities(new GetCitiesEvent
        {
            Latitude = 36.16589,
            Longitude = -86.78444,
            RadiusKm = 400,
            MaxCities = 2,
        });

        Assert.Equal(["Nashville", "Memphis"], response.Cities.Select(city => city.Name));
        Assert.Equal(5, response.TotalAvailable);
        Assert.Equal(2, response.Returned);
    }

    [SqlServerFact]
    public async Task Upsert_InsertsUpdatesAndCountsUnchangedRows()
    {
        var incoming = Cities.Where(city => city.Name != "London").ToList();
        incoming[0] = City(4644585, "Nashville", "TN", "PPLA", 36.16589, -86.78444, 720000);
        incoming.Add(City(4634946, "Knoxville", "TN", "PPLA2", 35.96064, -83.92074, 190740));

        await using var db = CreateDb();
        var response = await Upsert(db, incoming);

        Assert.Equal(1, response.Inserted);
        Assert.Equal(1, response.Updated);
        Assert.Equal(5, response.Unchanged);

        await using var verify = CreateDb();
        Assert.Equal(8, await verify.Cities.CountAsync());
        var nashville = await verify.Cities.SingleAsync(city => city.GeonameId == 4644585);
        Assert.Equal(720000, nashville.Population);
        Assert.Equal("Tennessee", nashville.Admin1Name);
    }

    [SqlServerFact]
    public async Task Handle_BulkDeletesRowsTheExportNoLongerLists()
    {
        // London drops out of the export, so the parent job ExecuteDeletes it from dbo.Cities.
        var lines = Cities.Where(city => city.Name != "London").Select(Line);
        var http = new GeoNamesHandler(Zip(string.Join('\n', lines)), "US.TN\tTennessee\tTennessee\t4662168\n");

        await using var db = CreateDb();
        var handler = new ImportCitiesHandler(
            db,
            new TransientRetryHelper(NullLogger<TransientRetryHelper>.Instance),
            new FakeHttpClientFactory(http),
            NullLogger<ImportCitiesHandler>.Instance,
            new DiscardingJobClient());
        var response = await handler.Handle(new ImportCitiesEvent(), CancellationToken.None);

        Assert.Equal(6, response.Downloaded);
        Assert.Equal(1, response.Deleted);

        await using var verify = CreateDb();
        Assert.Equal(6, await verify.Cities.CountAsync());
        Assert.False(await verify.Cities.AnyAsync(city => city.GeonameId == 2643743));
    }

    private async Task<NonAICitiesResponse> GetCities(GetCitiesEvent request)
    {
        await using var db = CreateDb();
        var handler = new GetCitiesHandler(
            new CacheHelper(new MemoryCache(new MemoryCacheOptions())),
            db,
            NullLogger<GetCitiesHandler>.Instance);
        return await handler.Handle(request, CancellationToken.None);
    }

    private WX1116DbContext CreateDb() =>
        new(new DbContextOptionsBuilder<WX1116DbContext>()
            .UseSqlServer(_connectionString, sql => sql.UseNetTopologySuite())
            .Options);

    private static Task<ImportCitiesUpsertResponse> Upsert(WX1116DbContext db, IEnumerable<GeoNamesCityDto> cities) =>
        new ImportCitiesUpsertHandler(db, NullLogger<ImportCitiesUpsertHandler>.Instance)
            .Handle(new ImportCitiesUpsertEvent { Cities = [.. cities] }, CancellationToken.None);

    // Each test gets its own database, so the tests never see each other's writes.
    private static string BuildConnectionString()
    {
        var builder = new SqlConnectionStringBuilder(SqlServerFactAttribute.ConnectionString ?? string.Empty)
        {
            InitialCatalog = $"CoreCityTests_{Guid.NewGuid():N}",
        };
        return builder.ConnectionString;
    }

    private static GeoNamesCityDto City(
        int geonameId, string name, string admin1Code, string featureCode, double latitude, double longitude, long population) =>
        new()
        {
            GeonameId = geonameId,
            Name = name,
            CountryCode = admin1Code == "ENG" ? "GB" : "US",
            Admin1Code = admin1Code,
            FeatureCode = featureCode,
            Latitude = latitude,
            Longitude = longitude,
            Population = population,
            Timezone = admin1Code == "ENG" ? "Europe/London" : "America/Chicago",
            Admin1Name = Admin1Names.GetValueOrDefault(admin1Code == "ENG" ? "GB.ENG" : $"US.{admin1Code}"),
        };

    /// <summary>Writes <paramref name="city"/> back out as a 19-column cities500.txt row.</summary>
    private static string Line(GeoNamesCityDto city) => FormattableString.Invariant(
        $"{city.GeonameId}\t{city.Name}\t{city.Name}\t\t{city.Latitude}\t{city.Longitude}\tP\t{city.FeatureCode}\t{city.CountryCode}\t\t{city.Admin1Code}\t\t\t\t{city.Population}\t\t\t{city.Timezone}\t2024-01-01");

    private static byte[] Zip(string citiesText)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            using var writer = new StreamWriter(archive.CreateEntry(ImportCitiesHandler.CitiesEntryName).Open());
            writer.Write(citiesText);
        }

        return stream.ToArray();
    }

    private sealed class FakeHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    /// <summary>Accepts the upsert batches without running them; this fact only checks the delete.</summary>
    private sealed class DiscardingJobClient : IBackgroundJobClient
    {
        public string Create(Job job, IState state) => Guid.NewGuid().ToString();

        public bool ChangeState(string jobId, IState state, string expectedState) => throw new NotSupportedException();
    }

    /// <summary>Serves the cities500 zip and admin1 codes text by URL.</summary>
    private sealed class GeoNamesHandler(byte[] citiesZip, string admin1Text) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpContent content = request.RequestUri!.ToString() == ImportCitiesHandler.CitiesUrl
                ? new ByteArrayContent(citiesZip)
                : new StringContent(admin1Text, Encoding.UTF8, "text/plain");

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
        }
    }
}
