using Core.Geo.Events;
using Core.Geo.Models;

namespace Core.Geo;

internal static class NonAILatLongMapper
{
    public static int NormalizeCount(int count) =>
        Math.Clamp(count, 1, GetLatLongEvent.MaxCount);

    public static NonAILatLongListResponse FromGeocodingResults(IReadOnlyList<NonAIGeocodingResult> matches)
    {
        ArgumentNullException.ThrowIfNull(matches);

        return new NonAILatLongListResponse
        {
            Results = matches.Select((match, index) => new NonAILatLongResponse
            {
                Rank = index + 1,
                Name = match.Name,
                State = match.Admin1,
                Country = match.Country,
                Latitude = match.Latitude,
                Longitude = match.Longitude,
            }).ToList(),
        };
    }
}
