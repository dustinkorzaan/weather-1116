using Core.Geo.Models;
using CQMediator;

namespace Core.Geo.Events;

/// <summary>
/// Downloads GeoNames' cities500 export and merges it (insert, update, delete) into dbo.City.
/// Scheduled daily by the worker's RecurringJobScheduler.
/// </summary>
public class ImportCitiesEvent : IRequest<ImportCitiesResponse>
{
}
