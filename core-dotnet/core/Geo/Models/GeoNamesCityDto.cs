namespace Core.Geo.Models;

/// <summary>
/// One row of GeoNames' cities500.txt, as parsed by ImportCitiesHandler. No Id or GeoPoint --
/// those only exist on the <see cref="Core.Data.Domain.City"/> domain entity.
/// </summary>
public class GeoNamesCityDto
{
    public int GeonameId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string CountryCode { get; set; } = string.Empty;

    public string Admin1Code { get; set; } = string.Empty;

    public string FeatureCode { get; set; } = string.Empty;

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public long Population { get; set; }

    public string Timezone { get; set; } = string.Empty;
}
