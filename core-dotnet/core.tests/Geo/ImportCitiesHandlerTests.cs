using System.IO.Compression;
using System.Net;
using System.Text;
using Core.Data;
using Core.Data.Domain;
using Core.Geo.Events;
using Core.Geo.Handlers;
using Core.Geo.Models;
using Core.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Core.Tests.Geo;

public class ImportCitiesHandlerTests
{
    // Real cities500.txt rows: tab-delimited, 19 columns, no header.
    private const string NashvilleLine = "4644585\tNashville\tNashville\tNashville,Nashville-Davidson\t36.16589\t-86.78444\tP\tPPLA\tUS\t\tTN\t037\t\t\t715884\t169\t165\tAmerica/Chicago\t2024-01-01";
    private const string ManhattanLine = "5125771\tManhattan\tManhattan\t\t40.78343\t-73.96625\tP\tPPLX\tUS\t\tNY\t061\t\t\t1694251\t0\t22\tAmerica/New_York\t2024-01-01";
    private const string AndorraLine = "3041563\tAndorra la Vella\tAndorra la Vella\t\t42.50779\t1.52109\tP\tPPLC\tAD\t\t07\t\t\t\t20430\t\t1037\tEurope/Andorra\t2020-03-03";

    private const string Admin1Text = "US.TN\tTennessee\tTennessee\t4662168\nUS.NY\tNew York\tNew York\t5128638\nAD.07\tAndorra la Vella\tAndorra la Vella\t3041566\n";

    [Fact]
    public void Parse_ReadsTheNeededColumns()
    {
        var city = Assert.Single(ImportCitiesHandler.Parse(new StringReader(NashvilleLine)));

        Assert.Equal(4644585, city.GeonameId);
        Assert.Equal("Nashville", city.Name);
        Assert.Equal("US", city.CountryCode);
        Assert.Equal("TN", city.Admin1Code);
        Assert.Equal("PPLA", city.FeatureCode);
        Assert.Equal(36.16589, city.Latitude);
        Assert.Equal(-86.78444, city.Longitude);
        Assert.Equal(715884, city.Population);
        Assert.Equal("America/Chicago", city.Timezone);
    }

    [Fact]
    public void Parse_KeepsEveryFeatureCodeAndSkipsMalformedLines()
    {
        var text = string.Join('\n', NashvilleLine, "not\ta\tcity", ManhattanLine, NashvilleLine.Replace("715884", "lots"), AndorraLine);

        var cities = ImportCitiesHandler.Parse(new StringReader(text)).ToList();

        Assert.Equal(["Nashville", "Manhattan", "Andorra la Vella"], cities.Select(city => city.Name));
        Assert.Equal("PPLX", cities[1].FeatureCode);
    }

    [Fact]
    public void ParseAdmin1Names_KeysOnCountryDotAdmin1()
    {
        var names = ImportCitiesHandler.ParseAdmin1Names(new StringReader(Admin1Text));

        Assert.Equal("Tennessee", names[ImportCitiesHandler.Admin1Key("US", "TN")]);
        Assert.Equal("Andorra la Vella", names["AD.07"]);
    }

    [Fact]
    public void ApplyChanges_NewCity_SetsEveryFieldAndGeoPoint()
    {
        var city = new City();
        var dto = Dto(NashvilleLine);

        Assert.True(ImportCitiesHandler.ApplyChanges(city, dto, "Tennessee"));

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
        ImportCitiesHandler.ApplyChanges(city, Dto(NashvilleLine), "Tennessee");
        var point = city.GeoPoint;

        Assert.False(ImportCitiesHandler.ApplyChanges(city, Dto(NashvilleLine), "Tennessee"));
        Assert.Same(point, city.GeoPoint);
    }

    [Fact]
    public void ApplyChanges_PopulationChange_KeepsGeoPoint()
    {
        var city = new City();
        ImportCitiesHandler.ApplyChanges(city, Dto(NashvilleLine), "Tennessee");
        var point = city.GeoPoint;
        var dto = Dto(NashvilleLine);
        dto.Population = 720000;

        Assert.True(ImportCitiesHandler.ApplyChanges(city, dto, "Tennessee"));
        Assert.Equal(720000, city.Population);
        Assert.Same(point, city.GeoPoint);
    }

    [Fact]
    public void ApplyChanges_LatLongChange_RebuildsGeoPoint()
    {
        var city = new City();
        ImportCitiesHandler.ApplyChanges(city, Dto(NashvilleLine), "Tennessee");
        var point = city.GeoPoint;
        var dto = Dto(NashvilleLine);
        dto.Latitude = 36.2;

        Assert.True(ImportCitiesHandler.ApplyChanges(city, dto, "Tennessee"));
        Assert.NotSame(point, city.GeoPoint);
        Assert.Equal(36.2, city.GeoPoint.Y);
        Assert.Equal(36.2, city.Latitude);
    }

    [Fact]
    public void ConfirmAdmin1Count_RejectsAnEmptyOrTruncatedList()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            ImportCitiesHandler.ConfirmAdmin1Count(ImportCitiesHandler.ParseAdmin1Names(new StringReader(Admin1Text))));

        Assert.Contains("held only 3 regions", ex.Message);
    }

    [Fact]
    public void ConfirmCityCount_RejectsAtOrUnderTheFloor()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            ImportCitiesHandler.ConfirmCityCount(ImportCitiesHandler.MinimumCityCount, 0));

        Assert.Contains("expected more than 100000", ex.Message);
    }

    [Theory]
    [InlineData(100_001, 0)]
    [InlineData(120_000, 0)]
    [InlineData(225_000, 230_000)]
    [InlineData(207_000, 230_000)]
    public void ConfirmCityCount_AcceptsAFirstLoadOrANearlyCompleteExport(int incoming, int existing) =>
        ImportCitiesHandler.ConfirmCityCount(incoming, existing);

    [Theory]
    [InlineData(120_000, 225_000)]
    [InlineData(206_999, 230_000)]
    public void ConfirmCityCount_RejectsALargeButPartialExport(int incoming, int existing)
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            ImportCitiesHandler.ConfirmCityCount(incoming, existing));

        Assert.Contains("already in dbo.Cities", ex.Message);
    }

    [Fact]
    public async Task Merge_PartialExport_ThrowsWithoutTouchingTheDatabase()
    {
        using var db = CreateDb();
        var nashville = NewCity(Dto(NashvilleLine));
        nashville.Population = 1;
        db.Cities.AddRange(nashville, NewCity(Dto(AndorraLine)), NewCity(Dto(ManhattanLine)));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var handler = CreateHandler(db, new GeoNamesHandler([], string.Empty), cityFloor: 0);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Merge([Dto(NashvilleLine)], new Dictionary<string, string>(), CancellationToken.None));

        Assert.Contains("already in dbo.Cities", ex.Message);
        Assert.Equal(3, await db.Cities.CountAsync());
        Assert.Equal(1, (await db.Cities.SingleAsync(city => city.GeonameId == 4644585)).Population);
    }

    [Fact]
    public async Task Merge_DuplicateGeonameId_ThrowsWithoutTouchingTheDatabase()
    {
        using var db = CreateDb();
        var handler = CreateHandler(db, new GeoNamesHandler([], string.Empty), cityFloor: 0);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Merge(
                [Dto(NashvilleLine), Dto(AndorraLine), Dto(NashvilleLine)],
                new Dictionary<string, string>(),
                CancellationToken.None));

        Assert.Contains("more than once", ex.Message);
        Assert.Equal(0, await db.Cities.CountAsync());
    }

    [Fact]
    public async Task Handle_TooFewCities_ThrowsWithoutTouchingTheDatabase()
    {
        using var db = CreateDb();
        db.Cities.Add(NewCity(Dto(NashvilleLine)));
        await db.SaveChangesAsync();
        var http = new GeoNamesHandler(Zip(string.Join('\n', NashvilleLine, AndorraLine)), ManyAdmin1Regions());
        var handler = CreateHandler(db, http);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new ImportCitiesEvent(), CancellationToken.None));

        Assert.Contains("held only 2 cities", ex.Message);
        Assert.Equal(1, await db.Cities.CountAsync());
        Assert.Equal(
            [ImportCitiesHandler.Admin1CodesUrl, ImportCitiesHandler.CitiesUrl],
            http.RequestedUrls);
    }

    [Fact]
    public async Task Handle_TooFewAdmin1Regions_ThrowsBeforeDownloadingCities()
    {
        using var db = CreateDb();
        var http = new GeoNamesHandler(Zip(NashvilleLine), Admin1Text);
        var handler = CreateHandler(db, http);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new ImportCitiesEvent(), CancellationToken.None));

        Assert.Contains("held only 3 regions", ex.Message);
        Assert.Equal(0, await db.Cities.CountAsync());
        Assert.Equal([ImportCitiesHandler.Admin1CodesUrl], http.RequestedUrls);
    }

    [Fact]
    public async Task Handle_StreamsTheZipTwiceAndUpsertsEachCity()
    {
        using var db = CreateDb();
        db.Cities.Add(NewCity(Dto(NashvilleLine)));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var http = new GeoNamesHandler(Zip(string.Join('\n', NashvilleLine, AndorraLine, ManhattanLine)), ManyAdmin1Regions() + Admin1Text);
        var handler = CreateHandler(db, http, cityFloor: 0);

        var response = await handler.Handle(new ImportCitiesEvent(), CancellationToken.None);

        Assert.Equal(3, response.Downloaded);
        Assert.Equal(2, response.Inserted);
        Assert.Equal(1, response.Updated);
        Assert.Equal("New York", (await db.Cities.SingleAsync(city => city.GeonameId == 5125771)).Admin1Name);
    }

    [Fact]
    public async Task Merge_InsertsUpdatesAndCountsUnchanged_ThenSavesNothingOnARepeat()
    {
        using var db = CreateDb();
        var nashville = NewCity(Dto(NashvilleLine));
        nashville.Admin1Name = "Tennessee";
        var andorra = NewCity(Dto(AndorraLine));
        andorra.Population = 1;
        db.Cities.AddRange(nashville, andorra);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var admin1Names = ImportCitiesHandler.ParseAdmin1Names(new StringReader(Admin1Text));
        var incoming = new[] { Dto(NashvilleLine), Dto(AndorraLine), Dto(ManhattanLine) };
        var handler = CreateHandler(db, new GeoNamesHandler([], string.Empty), cityFloor: 0);

        var first = await handler.Merge(incoming, admin1Names, CancellationToken.None);

        Assert.Equal(3, first.Downloaded);
        Assert.Equal(1, first.Inserted);
        Assert.Equal(1, first.Updated);
        Assert.Equal(1, first.Unchanged);
        Assert.Equal(0, first.Deleted);
        db.ChangeTracker.Clear();
        var saved = await db.Cities.OrderBy(city => city.GeonameId).ToListAsync();
        Assert.Equal(20430, saved.Single(city => city.GeonameId == 3041563).Population);
        var manhattan = saved.Single(city => city.GeonameId == 5125771);
        Assert.NotEqual(Guid.Empty, manhattan.Id);
        Assert.Equal("New York", manhattan.Admin1Name);

        var second = await handler.Merge(incoming, admin1Names, CancellationToken.None);

        Assert.Equal(0, second.Inserted);
        Assert.Equal(0, second.Updated);
        Assert.Equal(3, second.Unchanged);
        Assert.False(db.ChangeTracker.HasChanges());
    }

    private static GeoNamesCityDto Dto(string line) => ImportCitiesHandler.Parse(new StringReader(line)).Single();

    private static City NewCity(GeoNamesCityDto dto)
    {
        var city = new City { Id = Guid.NewGuid(), GeonameId = dto.GeonameId };
        ImportCitiesHandler.ApplyChanges(city, dto, null);
        return city;
    }

    private static string ManyAdmin1Regions() =>
        string.Concat(Enumerable.Range(1, ImportCitiesHandler.MinimumAdmin1Count)
            .Select(id => $"XX.{id}\tRegion {id}\tRegion {id}\t{id}\n"));

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

    private static WX1116DbContext CreateDb() =>
        new(new DbContextOptionsBuilder<WX1116DbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static ImportCitiesHandler CreateHandler(
        WX1116DbContext db, HttpMessageHandler http, int cityFloor = ImportCitiesHandler.MinimumCityCount) =>
        new(
            db,
            new TransientRetryHelper(NullLogger<TransientRetryHelper>.Instance),
            new FakeHttpClientFactory(http),
            NullLogger<ImportCitiesHandler>.Instance)
        {
            CityFloor = cityFloor,
        };

    private sealed class FakeHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    /// <summary>Serves the cities500 zip and admin1 codes text by URL.</summary>
    private sealed class GeoNamesHandler(byte[] citiesZip, string admin1Text) : HttpMessageHandler
    {
        public List<string> RequestedUrls { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri!.ToString();
            RequestedUrls.Add(url);
            HttpContent content = url == ImportCitiesHandler.CitiesUrl
                ? new ByteArrayContent(citiesZip)
                : new StringContent(admin1Text, Encoding.UTF8, "text/plain");

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
        }
    }
}
