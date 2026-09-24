using System.Globalization;
using Core.Data;
using Core.Geo.Events;
using Core.Geo.Models;
using Core.Geo.Services;
using CQMediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Geo.Handlers;

/// <summary>
/// The last job of an import-cities run: reads every GeonameId the run imported from blob storage,
/// queries dbo.Cities for its GeonameIds only, and deletes the ones GeoNames no longer lists in
/// batches of <see cref="ImportCitiesHandler.BatchSize"/>. The ids file is deleted once that finishes.
/// </summary>
public class ImportCitiesDeleteHandler : IRequestHandler<ImportCitiesDeleteEvent, ImportCitiesDeleteResponse>
{
    private readonly WX1116DbContext _db;
    private readonly ICityImportBlobStore? _blobs;
    private readonly ILogger<ImportCitiesDeleteHandler> _logger;

    // blobs is optional for the same reason as on ImportCitiesHandler: only the worker runs this.
    public ImportCitiesDeleteHandler(
        WX1116DbContext db,
        ILogger<ImportCitiesDeleteHandler> logger,
        ICityImportBlobStore? blobs = null)
    {
        _db = db;
        _blobs = blobs;
        _logger = logger;
    }

    public async Task<ImportCitiesDeleteResponse> Handle(ImportCitiesDeleteEvent request, CancellationToken cancellationToken)
    {
        var blobs = ImportCitiesHandler.RequireBlobs(_blobs);
        var importedIds = ParseIds(await blobs.DownloadTextAsync(request.GeonameIdsBlob, cancellationToken));

        // Only GeonameIds come back (about 1 MB for 250k rows), never whole City rows.
        var existingIds = await _db.Cities.Select(city => city.GeonameId).ToListAsync(cancellationToken);

        var response = new ImportCitiesDeleteResponse();
        foreach (var batch in MissingIdBatches(existingIds, importedIds))
        {
            response.Deleted += await _db.Cities
                .Where(city => batch.Contains(city.GeonameId))
                .ExecuteDeleteAsync(cancellationToken);
        }

        await blobs.DeleteAsync(request.GeonameIdsBlob, cancellationToken);

        _logger.LogInformation("ImportCitiesDelete: {Imported} imported, {Deleted} deleted", importedIds.Count, response.Deleted);

        return response;
    }

    /// <summary>GeonameIds in dbo.Cities that the export no longer lists, in batches of <see cref="ImportCitiesHandler.BatchSize"/>.</summary>
    internal static IEnumerable<int[]> MissingIdBatches(IEnumerable<int> existingIds, HashSet<int> importedIds) =>
        existingIds.Where(id => !importedIds.Contains(id)).Chunk(ImportCitiesHandler.BatchSize);

    internal static HashSet<int> ParseIds(string text) =>
        text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(id => int.Parse(id, CultureInfo.InvariantCulture))
            .ToHashSet();
}
