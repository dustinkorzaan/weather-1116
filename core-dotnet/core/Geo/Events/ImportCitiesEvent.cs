using Core.Geo.Models;
using CQMediator;

namespace Core.Geo.Events;

/// <summary>
/// Stages GeoNames' cities500 export in blob storage, one file per batch of cities, then enqueues one
/// <see cref="ImportCitiesUpsertEvent"/> job per file and a final <see cref="ImportCitiesDeleteEvent"/>.
/// Scheduled daily by the worker's RecurringJobScheduler.
/// </summary>
public class ImportCitiesEvent : IRequest<ImportCitiesResponse>
{
}
