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
/// (state/province) names, confirms it holds more than <see cref="MinimumCityCount"/> cities, then
/// merges it into dbo.Cities: updates changed rows, inserts new ones, and bulk-deletes rows GeoNames
/// no longer lists.
/// </summary>
public class ImportCitiesHandler : IRequestHandler<ImportCitiesEvent, ImportCitiesResponse>
{
    internal const string CitiesUrl = "https://download.geonames.org/export/dump/cities500.zip";
    internal const string CitiesEntryName = "cities500.txt";
    internal const string Admin1CodesUrl = "https://download.geonames.org/export/dump/admin1CodesASCII.txt";

    // A truncated or empty download must never wipe the table, so the merge only runs above this.
    internal const int MinimumCityCount = 100_000;

    // A download that is large but partial (or partly unparsable) would still pass the absolute
    // floor above and then bulk-delete every city it is missing, so once dbo.Cities holds data the
    // export must also keep at least this share of the current rows.
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

    public async Task<ImportCitiesResponse> Handle(ImportCitiesEvent request, CancellationToken cancellationToken)
    {
        using var client = _clientFactory.CreateClient();
        client.Timeout = TimeSpan.FromMinutes(5);
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", GetLocationHandler.UserAgent);

        var citiesZip = await Download(client, CitiesUrl, cancellationToken);
        var admin1Text = await Download(client, Admin1CodesUrl, cancellationToken);

        var incoming = ReadCitiesZip(citiesZip);
        using var admin1Reader = new StreamReader(new MemoryStream(admin1Text));
        var admin1Names = ParseAdmin1Names(admin1Reader);

        ConfirmCityCount(incoming);
        ConfirmAdmin1Count(admin1Names);

        var response = await Merge(incoming, admin1Names, cancellationToken);

        _logger.LogInformation(
            "ImportCities: {Downloaded} downloaded, {Inserted} inserted, {Updated} updated, {Deleted} deleted, {Unchanged} unchanged",
            response.Downloaded,
            response.Inserted,
            response.Updated,
            response.Deleted,
            response.Unchanged);

        return response;
    }

    internal async Task<ImportCitiesResponse> Merge(
        IReadOnlyList<GeoNamesCityDto> incoming,
        IReadOnlyDictionary<string, string> admin1Names,
        CancellationToken cancellationToken)
    {
        var response = new ImportCitiesResponse { Downloaded = incoming.Count };

        var existing = await _db.Cities.ToDictionaryAsync(city => city.GeonameId, cancellationToken);
        ConfirmShareOfExisting(incoming.Count, existing.Count);
        var inserts = new List<City>();

        foreach (var dto in incoming)
        {
            var admin1Name = admin1Names.GetValueOrDefault(Admin1Key(dto.CountryCode, dto.Admin1Code));

            if (existing.Remove(dto.GeonameId, out var city))
            {
                if (ApplyChanges(city, dto, admin1Name))
                {
                    response.Updated++;
                }
                else
                {
                    response.Unchanged++;
                }
            }
            else
            {
                city = new City { Id = Guid.NewGuid(), GeonameId = dto.GeonameId };
                ApplyChanges(city, dto, admin1Name);
                inserts.Add(city);
            }
        }

        _db.Cities.AddRange(inserts);
        response.Inserted = inserts.Count;

        if (_db.ChangeTracker.HasChanges())
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        // Whatever is left in `existing` is no longer in the GeoNames export.
        foreach (var chunk in existing.Keys.Chunk(DeleteChunkSize))
        {
            response.Deleted += await _db.Cities
                .Where(city => chunk.Contains(city.GeonameId))
                .ExecuteDeleteAsync(cancellationToken);
        }

        return response;
    }

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

    internal static void ConfirmCityCount(IReadOnlyList<GeoNamesCityDto> incoming)
    {
        if (incoming.Count <= MinimumCityCount)
        {
            throw new InvalidOperationException(
                $"GeoNames cities500 export held only {incoming.Count} cities (expected more than {MinimumCityCount}); import skipped.");
        }

        var duplicate = incoming.GroupBy(dto => dto.GeonameId).FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidOperationException(
                $"GeoNames cities500 export lists geonameid {duplicate.Key} more than once; import skipped.");
        }
    }

    internal static void ConfirmShareOfExisting(int incomingCount, int existingCount)
    {
        if (incomingCount < existingCount * MinimumShareOfExistingCities)
        {
            throw new InvalidOperationException(
                $"GeoNames cities500 export held {incomingCount} cities, under {MinimumShareOfExistingCities:P0} of the {existingCount} already in dbo.Cities; import skipped.");
        }
    }

    internal static void ConfirmAdmin1Count(IReadOnlyDictionary<string, string> admin1Names)
    {
        if (admin1Names.Count < MinimumAdmin1Count)
        {
            throw new InvalidOperationException(
                $"GeoNames admin1CodesASCII.txt held only {admin1Names.Count} regions (expected at least {MinimumAdmin1Count}); import skipped.");
        }
    }

    internal static List<GeoNamesCityDto> ReadCitiesZip(byte[] zipBytes)
    {
        using var archive = new ZipArchive(new MemoryStream(zipBytes), ZipArchiveMode.Read);
        var entry = archive.GetEntry(CitiesEntryName)
            ?? throw new InvalidOperationException($"GeoNames zip is missing {CitiesEntryName}.");

        using var reader = new StreamReader(entry.Open());
        return Parse(reader).ToList();
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

    private async Task<byte[]> Download(HttpClient client, string url, CancellationToken cancellationToken) =>
        await _retry.Execute(async ct =>
        {
            using var httpResponse = await client.GetAsync(url, ct);
            if (TransientRetryHelper.IsPermanentFailure(httpResponse.StatusCode))
            {
                // Not an HttpRequestException, so TransientRetryHelper does not retry it.
                throw new InvalidOperationException($"GeoNames rejected {url} with HTTP {(int)httpResponse.StatusCode}.");
            }

            httpResponse.EnsureSuccessStatusCode();
            return await httpResponse.Content.ReadAsByteArrayAsync(ct);
        }, cancellationToken);
}
