using Core.Geo.Models;
using CQMediator;

namespace Core.Geo.Events;

/// <summary>
/// Finds the largest cities (by population) within <see cref="RadiusKm"/> of a latitude/longitude,
/// at or above <see cref="MinPopulation"/>, returning at most <see cref="MaxCities"/>. Out-of-range
/// values are reset into range on every call instead of failing.
/// </summary>
public class GetCitiesEvent : IRequest<NonAICitiesResponse>
{
    public const double MinRadiusKm = 1;
    public const double DefaultRadiusKm = 161;
    public const double MaxRadiusKm = 1000;
    public const long DefaultMinPopulation = 0;
    public const int MinMaxCities = 0;
    public const int DefaultMaxCities = 25;
    public const int MaxMaxCities = 100;

    public required double Latitude { get; set; }

    public required double Longitude { get; set; }

    public double RadiusKm { get; set; } = DefaultRadiusKm;

    public long MinPopulation { get; set; } = DefaultMinPopulation;

    public int MaxCities { get; set; } = DefaultMaxCities;

    public static double NormalizeRadiusKm(double radiusKm) =>
        double.IsNaN(radiusKm) ? DefaultRadiusKm : Math.Clamp(radiusKm, MinRadiusKm, MaxRadiusKm);

    public static long NormalizeMinPopulation(long minPopulation) => Math.Max(minPopulation, 0);

    public static int NormalizeMaxCities(int maxCities) => Math.Clamp(maxCities, MinMaxCities, MaxMaxCities);
}
