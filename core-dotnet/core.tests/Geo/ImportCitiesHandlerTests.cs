using System.IO.Compression;
using System.Net;
using System.Text;
using Core.Geo.Events;
using Core.Geo.Handlers;
using Core.Geo.Models;
using Core.Hangfire;
using Core.Http;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
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
    public async Task Handle_StagesAdmin1BatchesAndIdsInBlobs_ThenEnqueuesUpsertsAndTheDeleteLast()
    {
        var lines = Enumerable.Range(1, ImportCitiesHandler.BatchSize * 2 + 1)
            .Select(id => NashvilleLine.Replace("4644585", id.ToString()))
            .Append(NashvilleLine.Replace("4644585", "1"))
            .ToList();
        var http = new GeoNamesHandler(Zip(string.Join('\n', lines)), Admin1Text);
        var blobs = new FakeCityImportBlobStore();
        var jobs = new RecordingJobClient(blobs);
        var handler = CreateHandler(http, jobs, blobs);

        var response = await handler.Handle(new ImportCitiesEvent(), CancellationToken.None);

        Assert.Equal(ImportCitiesHandler.BatchSize * 2 + 1, response.Downloaded);
        Assert.Equal(3, response.Enqueued);
        Assert.Equal([ImportCitiesHandler.Admin1CodesUrl, ImportCitiesHandler.CitiesUrl], http.RequestedUrls);
        Assert.All(jobs.Created, created => Assert.Equal(ImportCitiesHandler.ImportQueue, created.Queue));

        var events = jobs.Created.Select(created => HangfireCQMediatorEventSerializer.Deserialize(
            (string)created.Job.Args[1], (string)created.Job.Args[2])).ToList();
        var upserts = events.Take(3).Select(e => Assert.IsType<ImportCitiesUpsertEvent>(e)).ToList();
        var delete = Assert.IsType<ImportCitiesDeleteEvent>(events[^1]);

        // admin1 goes up unchanged and every batch points at it.
        Assert.Equal(Admin1Text, blobs.Blobs[upserts[0].Admin1Blob]);
        Assert.All(upserts, upsert => Assert.Equal(upserts[0].Admin1Blob, upsert.Admin1Blob));
        Assert.StartsWith("admin1codes", upserts[0].Admin1Blob);

        // Batch files hold raw cities500 lines: 1,000 / 1,000 / 1, the repeated id 1 skipped.
        var batches = upserts.Select(upsert => ImportCitiesHandler.Parse(new StringReader(blobs.Blobs[upsert.CitiesBlob])).ToList()).ToList();
        Assert.Equal([1_000, 1_000, 1], batches.Select(batch => batch.Count));
        Assert.Equal(Enumerable.Range(1, 2_001), batches.SelectMany(batch => batch).Select(city => city.GeonameId));
        Assert.All(upserts, upsert => Assert.StartsWith("cities", upsert.CitiesBlob));

        Assert.StartsWith("geonameids", delete.GeonameIdsBlob);
        Assert.Equal(Enumerable.Range(1, 2_001).ToHashSet(), ImportCitiesDeleteHandler.ParseIds(blobs.Blobs[delete.GeonameIdsBlob]));

        // Every job is enqueued only after every file is up.
        Assert.All(jobs.BlobCallsAtCreate, count => Assert.Equal(blobs.Calls.Count, count));
        Assert.Equal(5, blobs.Calls.Count);
    }

    [Fact]
    public async Task Handle_FailedCitiesDownload_EnqueuesNothing()
    {
        var http = new GeoNamesHandler([1, 2, 3], Admin1Text);
        var blobs = new FakeCityImportBlobStore();
        var jobs = new RecordingJobClient(blobs);
        var handler = CreateHandler(http, jobs, blobs);

        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(new ImportCitiesEvent(), CancellationToken.None));

        Assert.Empty(jobs.Created);
    }

    [Fact]
    public async Task Handle_WithoutBlobStorage_Throws()
    {
        var handler = new ImportCitiesHandler(
            new TransientRetryHelper(NullLogger<TransientRetryHelper>.Instance),
            new FakeHttpClientFactory(new GeoNamesHandler([], Admin1Text)),
            NullLogger<ImportCitiesHandler>.Instance,
            new RecordingJobClient(new FakeCityImportBlobStore()));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(new ImportCitiesEvent(), CancellationToken.None));

        Assert.Contains("blob storage", ex.Message);
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

    private static ImportCitiesHandler CreateHandler(HttpMessageHandler http, IBackgroundJobClient jobs, FakeCityImportBlobStore blobs) =>
        new(
            new TransientRetryHelper(NullLogger<TransientRetryHelper>.Instance),
            new FakeHttpClientFactory(http),
            NullLogger<ImportCitiesHandler>.Instance,
            jobs,
            blobs);

    private sealed class FakeHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    /// <summary>Records every job ImportCitiesHandler enqueues, and how many blob calls came before it.</summary>
    private sealed class RecordingJobClient(FakeCityImportBlobStore blobs) : IBackgroundJobClient
    {
        public List<(Job Job, string Queue)> Created { get; } = [];

        public List<int> BlobCallsAtCreate { get; } = [];

        public string Create(Job job, IState state)
        {
            Created.Add((job, Assert.IsType<EnqueuedState>(state).Queue));
            BlobCallsAtCreate.Add(blobs.Calls.Count);
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
