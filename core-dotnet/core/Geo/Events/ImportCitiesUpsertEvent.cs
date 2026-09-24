using Core.Geo.Models;
using CQMediator;

namespace Core.Geo.Events;

/// <summary>
/// Inserts or updates one batch of GeoNames cities in dbo.Cities, keyed on GeonameId. Enqueued by
/// ImportCitiesHandler as its own Hangfire job, so each batch commits and retries on its own; the
/// job carries only blob names, never the cities themselves.
/// </summary>
public class ImportCitiesUpsertEvent : IRequest<ImportCitiesUpsertResponse>
{
    /// <summary>Temp blob holding this batch as raw cities500.txt lines.</summary>
    public string CitiesBlob { get; set; } = string.Empty;

    /// <summary>Temp blob holding this run's admin1CodesASCII.txt, shared by every batch.</summary>
    public string Admin1Blob { get; set; } = string.Empty;
}
