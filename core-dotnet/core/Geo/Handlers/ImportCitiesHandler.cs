using System.Globalization;
using System.IO.Compression;
using Core.Data;
using Core.Geo.Events;
using Core.Geo.Models;
using Core.Hangfire;
using Core.Http;
using CQMediator;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Core.Geo.Handlers;

/// <summary>
/// Downloads GeoNames' cities500 export (every populated place with 500+ people) plus its admin1
/// (state/province) names and loads it into dbo.Cities without writing a city itself. The zip is
/// streamed to a temp file and read once, one row at a time; every <see cref="BatchSize"/> cities are
/// enqueued as their own <see cref="ImportCitiesUpsertEvent"/> Hangfire job, so each batch is short
/// and commits and retries on its own. The imported GeonameIds are kept, and rows GeoNames no longer
/// lists are bulk-deleted in batches of <see cref="BatchSize"/>.
/// </summary>
public class ImportCitiesHandler : IRequestHandler<ImportCitiesEvent, ImportCitiesResponse>
{
    internal const string CitiesUrl = "https://download.geonames.org/export/dump/cities500.zip";
    internal const string CitiesEntryName = "cities500.txt";
    internal const string Admin1CodesUrl = "https://download.geonames.org/export/dump/admin1CodesASCII.txt";
    internal const string UpsertQueue = "batch-single";
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

    private readonly WX1116DbContext _db;
    private readonly TransientRetryHelper _retry;
    private readonly IHttpClientFactory _clientFactory;
    private readonly IBackgroundJobClient? _backgroundJobs;
    private readonly ILogger<ImportCitiesHandler> _logger;

    // backgroundJobs is optional because CQMediator registers this handler in every Core host, and
    // hosts without Hangfire (the MCP servers) validate their DI container on startup. Only the
    // worker, which has Hangfire, ever runs this handler.
    public ImportCitiesHandler(
        WX1116DbContext db,
        TransientRetryHelper retry,
        IHttpClientFactory clientFactory,
        ILogger<ImportCitiesHandler> logger,
        IBackgroundJobClient? backgroundJobs = null)
    {
        _db = db;
        _retry = retry;
        _clientFactory = clientFactory;
        _backgroundJobs = backgroundJobs;
        _logger = logger;
    }

    public async Task<ImportCitiesResponse> Handle(ImportCitiesEvent request, CancellationToken cancellationToken)
    {
        var backgroundJobs = _backgroundJobs
            ?? throw new InvalidOperationException("ImportCities needs Hangfire (IBackgroundJobClient) to enqueue its upsert batches.");

        using var client = _clientFactory.CreateClient();
        client.Timeout = TimeSpan.FromMinutes(5);
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", GetLocationHandler.UserAgent);

        // admin1CodesASCII.txt is ~150 KB, so it is parsed in memory.
        var admin1Names = await _retry.Execute(async ct =>
        {
            using var httpResponse = await Get(client, Admin1CodesUrl, ct);
            using var admin1Reader = new StreamReader(await httpResponse.Content.ReadAsStreamAsync(ct));
            return ParseAdmin1Names(admin1Reader);
        }, cancellationToken);

        // ZipArchive needs a seekable stream, so the zip goes to a temp file rather than a byte[].
        await using var citiesZip = await _retry.Execute(ct => DownloadToTempFile(client, CitiesUrl, ct), cancellationToken);
        using var archive = new ZipArchive(citiesZip, ZipArchiveMode.Read);

        var response = new ImportCitiesResponse();
        var importedIds = new HashSet<int>();
        foreach (var batch in ReadCities(archive).Chunk(BatchSize))
        {
            foreach (var dto in batch)
            {
                importedIds.Add(dto.GeonameId);
                dto.Admin1Name = admin1Names.GetValueOrDefault(Admin1Key(dto.CountryCode, dto.Admin1Code));
            }

            backgroundJobs.EnqueueCQMediatorEvent(new ImportCitiesUpsertEvent { Cities = [.. batch] }, UpsertQueue);
            response.Downloaded += batch.Length;
            response.Enqueued++;
        }

        // Only GeonameIds come back (about 1 MB for 250k rows), never whole City rows.
        var existingIds = await _db.Cities.Select(city => city.GeonameId).ToListAsync(cancellationToken);
        foreach (var batch in MissingIdBatches(existingIds, importedIds))
        {
            response.Deleted += await _db.Cities
                .Where(city => batch.Contains(city.GeonameId))
                .ExecuteDeleteAsync(cancellationToken);
        }

        _logger.LogInformation(
            "ImportCities: {Downloaded} downloaded, {Enqueued} upsert batches enqueued, {Deleted} deleted",
            response.Downloaded,
            response.Enqueued,
            response.Deleted);

        return response;
    }

    /// <summary>GeonameIds in dbo.Cities that the export no longer lists, in batches of <see cref="BatchSize"/>.</summary>
    internal static IEnumerable<int[]> MissingIdBatches(IEnumerable<int> existingIds, HashSet<int> importedIds) =>
        existingIds.Where(id => !importedIds.Contains(id)).Chunk(BatchSize);

    /// <summary>Streams cities500.txt out of the zip one row at a time.</summary>
    private static IEnumerable<GeoNamesCityDto> ReadCities(ZipArchive archive)
    {
        var entry = archive.GetEntry(CitiesEntryName)
            ?? throw new InvalidOperationException($"GeoNames zip is missing {CitiesEntryName}.");

        using var reader = new StreamReader(entry.Open());
        foreach (var city in Parse(reader))
        {
            yield return city;
        }
    }

    /// <summary>Parses cities500.txt; lines with the wrong column count or unparsable numbers are skipped.</summary>
    internal static IEnumerable<GeoNamesCityDto> Parse(TextReader reader)
    {
        while (reader.ReadLine() is { } line)
        {
            var columns = line.Split('\t');
            if (columns.Length != ColumnCount
                || !int.TryParse(columns[GeonameIdColumn], NumberStyles.Integer, CultureInfo.InvariantCulture, out var geonameId)
                || !double.TryParse(columns[LatitudeColumn], NumberStyles.Float, CultureInfo.InvariantCulture, out var latitude)
                || !double.TryParse(columns[LongitudeColumn], NumberStyles.Float, CultureInfo.InvariantCulture, out var longitude)
                || !long.TryParse(columns[PopulationColumn], NumberStyles.Integer, CultureInfo.InvariantCulture, out var population))
            {
                continue;
            }

            yield return new GeoNamesCityDto
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
