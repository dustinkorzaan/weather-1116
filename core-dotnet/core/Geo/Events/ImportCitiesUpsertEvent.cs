using Core.Geo.Models;
using CQMediator;

namespace Core.Geo.Events;

/// <summary>
/// Inserts or updates one batch of GeoNames cities in dbo.Cities, keyed on GeonameId.
/// Enqueued by ImportCitiesHandler as its own Hangfire job, so each batch commits and retries on its own.
/// </summary>
public class ImportCitiesUpsertEvent : IRequest<ImportCitiesUpsertResponse>
{
    public List<GeoNamesCityDto> Cities { get; set; } = [];
}
