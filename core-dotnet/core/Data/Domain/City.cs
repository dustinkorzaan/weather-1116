using NetTopologySuite.Geometries;

namespace Core.Data.Domain;

/// <summary>
/// A populated place from GeoNames' cities500 export, loaded daily by ImportCitiesHandler.
/// <see cref="Latitude"/>/<see cref="Longitude"/> duplicate <see cref="GeoPoint"/> for readability.
/// </summary>
public class City
{
    public Guid Id { get; set; }

    public int GeonameId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string CountryCode { get; set; } = string.Empty;

    public string Admin1Code { get; set; } = string.Empty;

    public string? Admin1Name { get; set; }

    public string FeatureCode { get; set; } = string.Empty;

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public long Population { get; set; }

    public string Timezone { get; set; } = string.Empty;

    /// <summary>SRID 4326 geography point: X = longitude, Y = latitude.</summary>
    public Point GeoPoint { get; set; } = null!;
}
