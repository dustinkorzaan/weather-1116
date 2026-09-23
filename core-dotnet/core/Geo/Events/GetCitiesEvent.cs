using Core.Geo.Models;
using CQMediator;

namespace Core.Geo.Events;

/// <summary>
/// Finds the largest cities (by population) within <see cref="DistanceKm"/> of a latitude/longitude.
/// Out-of-range <see cref="DistanceKm"/>/<see cref="Size"/> values are reset into range on every
/// call instead of failing.
/// </summary>
public class GetCitiesEvent : IRequest<NonAICitiesResponse>
{
    public const double MinDistanceKm = 1;
    public const double DefaultDistanceKm = 161;
    public const double MaxDistanceKm = 1000;
    public const int MinSize = 0;
    public const int DefaultSize = 25;
    public const int MaxSize = 100;

    public required double Latitude { get; set; }

    public required double Longitude { get; set; }

    public double DistanceKm { get; set; } = DefaultDistanceKm;

    public int Size { get; set; } = DefaultSize;

    public static double NormalizeDistanceKm(double distanceKm) =>
        double.IsNaN(distanceKm) ? DefaultDistanceKm : Math.Clamp(distanceKm, MinDistanceKm, MaxDistanceKm);

    public static int NormalizeSize(int size) => Math.Clamp(size, MinSize, MaxSize);
}
