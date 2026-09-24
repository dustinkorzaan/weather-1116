using System.IO.Compression;
using System.Net;
using System.Text;
using Core.Data;
using Core.Geo.Events;
using Core.Geo.Handlers;
using Core.Geo.Models;
using Core.Hangfire;
using Core.Http;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
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
    public async Task Handle_EnqueuesOneUpsertJobPerBatchWithAdmin1Names()
    {
        using var db = CreateDb();
        var lines = Enumerable.Range(1, ImportCitiesHandler.BatchSize * 2 + 1)
            .Select(id => NashvilleLine.Replace("4644585", id.ToString()));
        var http = new GeoNamesHandler(Zip(string.Join('\n', lines)), Admin1Text);
        var jobs = new RecordingJobClient();
        var handler = CreateHandler(db, http, jobs);

        var response = await handler.Handle(new ImportCitiesEvent(), CancellationToken.None);

        Assert.Equal(ImportCitiesHandler.BatchSize * 2 + 1, response.Downloaded);
        Assert.Equal(3, response.Enqueued);
        Assert.Equal(0, response.Deleted);
        Assert.All(jobs.Created, created => Assert.Equal(ImportCitiesHandler.UpsertQueue, created.Queue));
        var events = jobs.Created.Select(created => Assert.IsType<ImportCitiesUpsertEvent>(
            HangfireCQMediatorEventSerializer.Deserialize((string)created.Job.Args[1], (string)created.Job.Args[2]))).ToList();
        Assert.Equal([1_000, 1_000, 1], events.Select(e => e.Cities.Count));
        Assert.All(events.SelectMany(e => e.Cities), city => Assert.Equal("Tennessee", city.Admin1Name));
        Assert.Equal(0, await db.Cities.CountAsync());
        Assert.Equal([ImportCitiesHandler.Admin1CodesUrl, ImportCitiesHandler.CitiesUrl], http.RequestedUrls);
    }

    [Fact]
    public void MissingIdBatches_SkipsImportedIdsAndSplitsTheRest()
    {
        var existing = Enumerable.Range(1, 3_000);
        var imported = Enumerable.Range(1, 500).ToHashSet();

        var batches = ImportCitiesHandler.MissingIdBatches(existing, imported).ToList();

        Assert.Equal([1_000, 1_000, 500], batches.Select(batch => batch.Length));
        Assert.Equal(501, batches[0][0]);
        Assert.Equal(3_000, batches[^1][^1]);
    }

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

    private static ImportCitiesHandler CreateHandler(WX1116DbContext db, HttpMessageHandler http, IBackgroundJobClient jobs) =>
        new(
            db,
            new TransientRetryHelper(NullLogger<TransientRetryHelper>.Instance),
            new FakeHttpClientFactory(http),
            NullLogger<ImportCitiesHandler>.Instance,
            jobs);

    private sealed class FakeHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    /// <summary>Records every job ImportCitiesHandler enqueues instead of storing it.</summary>
    private sealed class RecordingJobClient : IBackgroundJobClient
    {
        public List<(Job Job, string Queue)> Created { get; } = [];

        public string Create(Job job, IState state)
        {
            Created.Add((job, Assert.IsType<EnqueuedState>(state).Queue));
            return Created.Count.ToString();
        }

        public bool ChangeState(string jobId, IState state, string expectedState) => throw new NotSupportedException();
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
