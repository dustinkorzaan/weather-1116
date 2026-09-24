using Core.Geo.Models;
using CQMediator;

namespace Core.Geo.Events;

/// <summary>
/// Streams GeoNames' cities500 export, enqueues one <see cref="ImportCitiesUpsertEvent"/> job per
/// batch of cities, then bulk-deletes dbo.Cities rows no longer listed.
/// Scheduled daily by the worker's RecurringJobScheduler.
/// </summary>
public class ImportCitiesEvent : IRequest<ImportCitiesResponse>
{
}
