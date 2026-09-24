using System.Globalization;
using System.IO.Compression;
using System.Text;
using Core.Geo.Events;
using Core.Geo.Models;
using Core.Geo.Services;
using Core.Hangfire;
using Core.Http;
using CQMediator;
using Hangfire;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Core.Geo.Handlers;

/// <summary>
/// Downloads GeoNames' cities500 export (every populated place with 500+ people) plus its admin1
/// (state/province) names and stages them in blob storage without writing a city itself. admin1 goes
/// up as it is; the zip is streamed through a temp file one row at a time, a repeated GeonameId is
/// skipped, and every <see cref="BatchSize"/> unique rows go up as their own file of raw lines. The
/// imported GeonameIds go up as one more file. Only after every upload succeeds does it enqueue one
/// <see cref="ImportCitiesUpsertEvent"/> per batch file and then one <see cref="ImportCitiesDeleteEvent"/>,
/// all on <see cref="ImportQueue"/>, so each Hangfire job carries only blob names.
/// </summary>
public class ImportCitiesHandler : IRequestHandler<ImportCitiesEvent, ImportCitiesResponse>
{
    internal const string CitiesUrl = "https://download.geonames.org/export/dump/cities500.zip";
    internal const string CitiesEntryName = "cities500.txt";
    internal const string Admin1CodesUrl = "https://download.geonames.org/export/dump/admin1CodesASCII.txt";

    // One worker (WorkerCount = 1) and FIFO, so the delete job enqueued last runs after every upsert.
    internal const string ImportQueue = "batch-single";
    internal const int BatchSize = 1_000;

    // cities500.txt is tab-delimited with 19 columns and no header row.
    private const int ColumnCount = 19;
    private const int GeonameIdColumn = 0;
    private const int NameColumn = 1;
    private const int LatitudeColumn = 4;
    private const int LongitudeColumn = 5;
    private const int FeatureCodeColumn = 7;
    private const int CountryCodeColumn = 8;
    private const int Admin1CodeColumn = 10;
    private const int PopulationColumn = 14;
    private const int TimezoneColumn = 17;

    private readonly TransientRetryHelper _retry;
    private readonly IHttpClientFactory _clientFactory;
    private readonly IBackgroundJobClient? _backgroundJobs;
    private readonly ICityImportBlobStore? _blobs;
    private readonly ILogger<ImportCitiesHandler> _logger;

    // backgroundJobs and blobs are optional because CQMediator registers this handler in every Core
    // host, and hosts without Hangfire or blob settings (the MCP servers, api, mvc) validate their DI
    // container on startup. Only the worker, which has both, ever runs this handler.
    public ImportCitiesHandler(
        TransientRetryHelper retry,
        IHttpClientFactory clientFactory,
        ILogger<ImportCitiesHandler> logger,
        IBackgroundJobClient? backgroundJobs = null,
        ICityImportBlobStore? blobs = null)
    {
        _retry = retry;
        _clientFactory = clientFactory;
        _backgroundJobs = backgroundJobs;
        _blobs = blobs;
        _logger = logger;
    }

    public async Task<ImportCitiesResponse> Handle(ImportCitiesEvent request, CancellationToken cancellationToken)
    {
        var backgroundJobs = _backgroundJobs
            ?? throw new InvalidOperationException("ImportCities needs Hangfire (IBackgroundJobClient) to enqueue its upsert and delete jobs.");
        var blobs = RequireBlobs(_blobs);

        using var client = _clientFactory.CreateClient();
        client.Timeout = TimeSpan.FromMinutes(5);
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", GetLocationHandler.UserAgent);

        // admin1CodesASCII.txt is ~150 KB, so it goes up to blob storage in one piece.
        var admin1Text = await _retry.Execute(async ct =>
        {
            using var httpResponse = await Get(client, Admin1CodesUrl, ct);
            return await httpResponse.Content.ReadAsStringAsync(ct);
        }, cancellationToken);
        var admin1Blob = NewBlobName("admin1codes");
        await blobs.UploadTextAsync(admin1Blob, admin1Text, cancellationToken);

        // ZipArchive needs a seekable stream, so the zip goes to a temp file rather than a byte[].
        await using var citiesZip = await _retry.Execute(ct => DownloadToTempFile(client, CitiesUrl, ct), cancellationToken);
        using var archive = new ZipArchive(citiesZip, ZipArchiveMode.Read);

        var importedIds = new HashSet<int>();
        var citiesBlobs = new List<string>();
        var batch = new StringBuilder();
        var batchCount = 0;
        foreach (var (line, city) in ReadCities(archive))
        {
            // A GeonameId already seen is skipped, so no batch ever inserts the same city twice.
            if (!importedIds.Add(city.GeonameId))
            {
                continue;
            }

            batch.Append(line).Append('\n');
            if (++batchCount == BatchSize)
            {
                citiesBlobs.Add(await UploadBatch(blobs, batch, cancellationToken));
                batchCount = 0;
            }
        }

        if (batchCount > 0)
        {
            citiesBlobs.Add(await UploadBatch(blobs, batch, cancellationToken));
        }

        var geonameIdsBlob = NewBlobName("geonameids");
        await blobs.UploadTextAsync(geonameIdsBlob, string.Join('\n', importedIds), cancellationToken);

        // Nothing is enqueued until every file is up, so a failed download or upload leaves no jobs behind.
        foreach (var citiesBlob in citiesBlobs)
        {
            backgroundJobs.EnqueueCQMediatorEvent(
                new ImportCitiesUpsertEvent { CitiesBlob = citiesBlob, Admin1Blob = admin1Blob },
                ImportQueue);
        }

        backgroundJobs.EnqueueCQMediatorEvent(new ImportCitiesDeleteEvent { GeonameIdsBlob = geonameIdsBlob }, ImportQueue);

        var response = new ImportCitiesResponse { Downloaded = importedIds.Count, Enqueued = citiesBlobs.Count };
        _logger.LogInformation(
            "ImportCities: {Downloaded} downloaded, {Enqueued} upsert batches enqueued, delete enqueued",
            response.Downloaded,
            response.Enqueued);

        return response;
    }

    internal static ICityImportBlobStore RequireBlobs(ICityImportBlobStore? blobs) =>
        blobs ?? throw new InvalidOperationException("ImportCities needs blob storage (BLOB_STORAGE_URL or BLOB_CONNECTION_STRING) to stage its batches.");

    internal static string NewBlobName(string prefix) => $"{prefix}{Guid.NewGuid():N}.txt";

    private static async Task<string> UploadBatch(ICityImportBlobStore blobs, StringBuilder batch, CancellationToken cancellationToken)
    {
        var blobName = NewBlobName("cities");
        await blobs.UploadTextAsync(blobName, batch.ToString(), cancellationToken);
        batch.Clear();
        return blobName;
    }

    /// <summary>Streams cities500.txt out of the zip one row at a time, with each row's raw line.</summary>
    private static IEnumerable<(string Line, GeoNamesCityDto City)> ReadCities(ZipArchive archive)
    {
        var entry = archive.GetEntry(CitiesEntryName)
            ?? throw new InvalidOperationException($"GeoNames zip is missing {CitiesEntryName}.");

        using var reader = new StreamReader(entry.Open());
        while (reader.ReadLine() is { } line)
        {
            if (ParseLine(line) is { } city)
            {
                yield return (line, city);
            }
        }
    }

    /// <summary>Parses cities500.txt; lines with the wrong column count or unparsable numbers are skipped.</summary>
    internal static IEnumerable<GeoNamesCityDto> Parse(TextReader reader)
    {
        while (reader.ReadLine() is { } line)
        {
            if (ParseLine(line) is { } city)
            {
                yield return city;
            }
        }
    }

    /// <summary>Parses one cities500.txt line, or returns null for the wrong column count or unparsable numbers.</summary>
    internal static GeoNamesCityDto? ParseLine(string line)
    {
        var columns = line.Split('\t');
        if (columns.Length != ColumnCount
            || !int.TryParse(columns[GeonameIdColumn], NumberStyles.Integer, CultureInfo.InvariantCulture, out var geonameId)
            || !double.TryParse(columns[LatitudeColumn], NumberStyles.Float, CultureInfo.InvariantCulture, out var latitude)
            || !double.TryParse(columns[LongitudeColumn], NumberStyles.Float, CultureInfo.InvariantCulture, out var longitude)
            || !long.TryParse(columns[PopulationColumn], NumberStyles.Integer, CultureInfo.InvariantCulture, out var population))
        {
            return null;
        }

        return new GeoNamesCityDto
        {
            GeonameId = geonameId,
            Name = columns[NameColumn],
            CountryCode = columns[CountryCodeColumn],
            Admin1Code = columns[Admin1CodeColumn],
            FeatureCode = columns[FeatureCodeColumn],
            Latitude = latitude,
            Longitude = longitude,
            Population = population,
            Timezone = columns[TimezoneColumn],
        };
    }

    /// <summary>Parses admin1CodesASCII.txt ("US.TN\tTennessee\tTennessee\t4662168") into "US.TN" → "Tennessee".</summary>
    internal static Dictionary<string, string> ParseAdmin1Names(TextReader reader)
    {
        var names = new Dictionary<string, string>(StringComparer.Ordinal);
        while (reader.ReadLine() is { } line)
        {
            var columns = line.Split('\t');
            if (columns.Length >= 2 && !string.IsNullOrWhiteSpace(columns[0]))
            {
                names[columns[0]] = columns[1];
            }
        }

        return names;
    }

    internal static string Admin1Key(string countryCode, string admin1Code) => $"{countryCode}.{admin1Code}";

    private static async Task<HttpResponseMessage> Get(HttpClient client, string url, CancellationToken cancellationToken)
    {
        var httpResponse = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (TransientRetryHelper.IsPermanentFailure(httpResponse.StatusCode))
        {
            httpResponse.Dispose();
            // Not an HttpRequestException, so TransientRetryHelper does not retry it.
            throw new InvalidOperationException($"GeoNames rejected {url} with HTTP {(int)httpResponse.StatusCode}.");
        }

        try
        {
            httpResponse.EnsureSuccessStatusCode();
            return httpResponse;
        }
        catch
        {
            httpResponse.Dispose();
            throw;
        }
    }

    /// <summary>Streams <paramref name="url"/> into a temp file that is deleted when the returned stream closes.</summary>
    private static async Task<FileStream> DownloadToTempFile(HttpClient client, string url, CancellationToken cancellationToken)
    {
        using var httpResponse = await Get(client, url, cancellationToken);
        var file = new FileStream(
            Path.GetTempFileName(),
            FileMode.Create,
            FileAccess.ReadWrite,
            FileShare.None,
            bufferSize: 81_920,
            FileOptions.DeleteOnClose | FileOptions.Asynchronous);
        try
        {
            await httpResponse.Content.CopyToAsync(file, cancellationToken);
            file.Position = 0;
            return file;
        }
        catch
        {
            await file.DisposeAsync();
            throw;
        }
    }
}
