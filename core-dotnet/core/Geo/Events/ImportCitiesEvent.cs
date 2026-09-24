using Core.Geo.Models;
using CQMediator;

namespace Core.Geo.Events;

/// <summary>
/// Streams GeoNames' cities500 export into dbo.Cities: validates it (100k+ cities, no duplicates) before
/// any write, upserts one row at a time, then bulk-deletes cities no longer listed.
/// Scheduled daily by the worker's RecurringJobScheduler.
/// </summary>
public class ImportCitiesEvent : IRequest<ImportCitiesResponse>
{
}
