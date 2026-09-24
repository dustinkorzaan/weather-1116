using Core.Geo.Models;
using CQMediator;

namespace Core.Geo.Events;

/// <summary>
/// Deletes the dbo.Cities rows GeoNames no longer lists. Enqueued by ImportCitiesHandler after every
/// <see cref="ImportCitiesUpsertEvent"/> on the same single-worker queue, so it runs last.
/// </summary>
public class ImportCitiesDeleteEvent : IRequest<ImportCitiesDeleteResponse>
{
    /// <summary>Temp blob holding every GeonameId the run imported, one per line.</summary>
    public string GeonameIdsBlob { get; set; } = string.Empty;
}
