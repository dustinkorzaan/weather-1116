using Core.Geo.Models;
using CQMediator;

namespace Core.Geo.Events;

/// <summary>
/// Streams GeoNames' cities500 export into dbo.Cities one row at a time (insert/update), then bulk-deletes
/// cities no longer listed once more than 100k were imported.
/// Scheduled daily by the worker's RecurringJobScheduler.
/// </summary>
public class ImportCitiesEvent : IRequest<ImportCitiesResponse>
{
}
