using System.Globalization;
using System.IO.Compression;
using Core.Data;
using Core.Data.Domain;
using Core.Geo.Events;
using Core.Geo.Models;
using Core.Http;
using CQMediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;

namespace Core.Geo.Handlers;

/// <summary>
/// Downloads GeoNames' cities500 export (every populated place with 500+ people) plus its admin1
/// (state/province) names and merges it into dbo.Cities. The zip is streamed to a temp file and its
/// rows are read one at a time: each row is looked up by GeonameId and inserted or updated on its
/// own, so memory stays flat no matter how large the export grows. Only the imported GeonameIds are
/// kept, and once more than <see cref="MinimumCityCount"/> cities were imported the rows GeoNames
/// no longer lists are bulk-deleted.
/// </summary>
public class ImportCitiesHandler : IRequestHandler<ImportCitiesEvent, ImportCitiesResponse>
{
    internal const string CitiesUrl = "https://download.geonames.org/export/dump/cities500.zip";
    internal const string CitiesEntryName = "cities500.txt";
    internal const string Admin1CodesUrl = "https://download.geonames.org/export/dump/admin1CodesASCII.txt";

    // A truncated or empty download must never wipe the table, so the bulk delete only runs above this.
    internal const int MinimumCityCount = 100_000;

    // A download that is large but partial (or partly unparsable) would still pass the absolute
    // floor above and then bulk-delete every city it is missing, so once dbo.Cities holds data the
    // export must also keep at least this share of the rows that were there before the import.
    internal const double MinimumShareOfExistingCities = 0.9;

    // admin1CodesASCII.txt lists about 3,900 regions; an empty or broken body would otherwise blank
    // every city's Admin1Name (GetCities' region) until the next good run.
    internal const int MinimumAdmin1Count = 1_000;
    internal const int DeleteChunkSize = 2_000;
    internal const int Srid = 4326;

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
    private readonly ILogger<ImportCitiesHandler> _logger;

    public ImportCitiesHandler(
        WX1116DbContext db,
        TransientRetryHelper retry,
        IHttpClientFactory clientFactory,
        ILogger<ImportCitiesHandler> logger)
    {
        _db = db;
        _retry = retry;
        _clientFactory = clientFactory;
        _logger = logger;
    }

    /// <summary>Imported-city floor for the bulk delete; tests lower it to exercise deletes on a few rows.</summary>
    internal int DeleteFloor { get; init; } = MinimumCityCount;

    public async Task<ImportCitiesResponse> Handle(ImportCitiesEvent request, CancellationToken cancellationToken)
    {
        using var client = _clientFactory.CreateClient();
        client.Timeout = TimeSpan.FromMinutes(5);
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", GetLocationHandler.UserAgent);

        // admin1CodesASCII.txt is ~150 KB, so it is parsed in memory and checked before any write.
        var admin1Names = await _retry.Execute(async ct =>
        {
            using var httpResponse = await Get(client, Admin1CodesUrl, ct);
            using var admin1Reader = new StreamReader(await httpResponse.Content.ReadAsStreamAsync(ct));
            return ParseAdmin1Names(admin1Reader);
        }, cancellationToken);
        ConfirmAdmin1Count(admin1Names);

        // ZipArchive needs a seekable stream, so the zip goes to a temp file rather than a byte[].
        await using var citiesZip = await _retry.Execute(ct => DownloadToTempFile(client, CitiesUrl, ct), cancellationToken);
        using var archive = new ZipArchive(citiesZip, ZipArchiveMode.Read);
        var entry = archive.GetEntry(CitiesEntryName)
            ?? throw new InvalidOperationException($"GeoNames zip is missing {CitiesEntryName}.");
        using var reader = new StreamReader(entry.Open());

        var response = await Merge(Parse(reader), admin1Names, cancellationToken);

        LogResponse(response);
        return response;
    }

    /// <summary>
    /// Upserts each city in <paramref name="incoming"/> one at a time (one lookup, and a save only
    /// when something changed), recording its GeonameId. Afterwards, provided more than
    /// <see cref="DeleteFloor"/> cities were imported and they cover
    /// <see cref="MinimumShareOfExistingCities"/> of the rows that existed beforehand, rows whose
    /// GeonameId was not imported are bulk-deleted; otherwise the delete is skipped and this throws.
    /// </summary>
    internal async Task<ImportCitiesResponse> Merge(
        IEnumerable<GeoNamesCityDto> incoming,
        IReadOnlyDictionary<string, string> admin1Names,
        CancellationToken cancellationToken)
    {
        var response = new ImportCitiesResponse();
        var existingCount = await _db.Cities.CountAsync(cancellationToken);
        var importedIds = new HashSet<int>();

        foreach (var dto in incoming)
        {
            response.Downloaded++;
            if (!importedIds.Add(dto.GeonameId))
            {
                // First row wins; a repeat would otherwise overwrite it with whatever came later.
                _logger.LogWarning("ImportCities: geonameid {GeonameId} listed more than once; later row skipped", dto.GeonameId);
                continue;
            }

            await Upsert(dto, admin1Names, response, cancellationToken);
        }

        ConfirmCanDelete(importedIds.Count, existingCount, response);

        // Only GeonameIds come back (about 1 MB for 250k rows), never whole City rows.
        var existingIds = await _db.Cities.Select(city => city.GeonameId).ToListAsync(cancellationToken);
        var missing = existingIds.Where(id => !importedIds.Contains(id)).ToList();

        foreach (var chunk in missing.Chunk(DeleteChunkSize))
        {
            response.Deleted += await _db.Cities
                .Where(city => chunk.Contains(city.GeonameId))
                .ExecuteDeleteAsync(cancellationToken);
        }

        return response;
    }

    private async Task Upsert(
        GeoNamesCityDto dto,
        IReadOnlyDictionary<string, string> admin1Names,
        ImportCitiesResponse response,
        CancellationToken cancellationToken)
    {
        var admin1Name = admin1Names.GetValueOrDefault(Admin1Key(dto.CountryCode, dto.Admin1Code));
        var city = await _db.Cities.FirstOrDefaultAsync(existing => existing.GeonameId == dto.GeonameId, cancellationToken);

        if (city is null)
        {
            city = new City { Id = Guid.NewGuid(), GeonameId = dto.GeonameId };
            ApplyChanges(city, dto, admin1Name);
            _db.Cities.Add(city);
            response.Inserted++;
        }
        else if (ApplyChanges(city, dto, admin1Name))
        {
            response.Updated++;
        }
        else
        {
            response.Unchanged++;
        }

        if (_db.ChangeTracker.HasChanges())
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        // Nothing is kept tracked between rows, so the context never grows with the export.
        _db.ChangeTracker.Clear();
    }

    private void ConfirmCanDelete(int importedCount, int existingCount, ImportCitiesResponse response)
    {
        string? reason = null;
        if (importedCount <= DeleteFloor)
        {
            reason = $"GeoNames cities500 export held only {importedCount} cities (expected more than {DeleteFloor})";
        }
        else if (importedCount < existingCount * MinimumShareOfExistingCities)
        {
            reason = $"GeoNames cities500 export held {importedCount} cities, under {MinimumShareOfExistingCities:P0} of the {existingCount} already in dbo.Cities";
        }

        if (reason is not null)
        {
            LogResponse(response);
            throw new InvalidOperationException($"{reason}; rows were upserted but the delete was skipped.");
        }
    }

    private void LogResponse(ImportCitiesResponse response) =>
        _logger.LogInformation(
            "ImportCities: {Downloaded} downloaded, {Inserted} inserted, {Updated} updated, {Deleted} deleted, {Unchanged} unchanged",
            response.Downloaded,
            response.Inserted,
            response.Updated,
            response.Deleted,
            response.Unchanged);

    /// <summary>
    /// Copies <paramref name="dto"/> onto <paramref name="city"/>, assigning only fields whose value
    /// differs so EF change tracking marks just the columns that really changed. Returns whether
    /// anything changed.
    /// </summary>
    internal static bool ApplyChanges(City city, GeoNamesCityDto dto, string? admin1Name)
    {
        var changed = false;

        if (city.Name != dto.Name) { city.Name = dto.Name; changed = true; }
        if (city.CountryCode != dto.CountryCode) { city.CountryCode = dto.CountryCode; changed = true; }
        if (city.Admin1Code != dto.Admin1Code) { city.Admin1Code = dto.Admin1Code; changed = true; }
        if (city.Admin1Name != admin1Name) { city.Admin1Name = admin1Name; changed = true; }
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

    internal static void ConfirmAdmin1Count(IReadOnlyDictionary<string, string> admin1Names)
    {
        if (admin1Names.Count < MinimumAdmin1Count)
        {
            throw new InvalidOperationException(
                $"GeoNames admin1CodesASCII.txt held only {admin1Names.Count} regions (expected at least {MinimumAdmin1Count}); import skipped.");
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
