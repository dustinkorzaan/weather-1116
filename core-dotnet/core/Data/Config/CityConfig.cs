using Core.Data.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Data.Config;

public class CityConfig : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> builder)
    {
        builder.ToTable("City", "dbo");
        builder.HasKey(city => city.Id);
        builder.Property(city => city.Id).ValueGeneratedNever();

        builder.Property(city => city.GeonameId);
        builder.HasIndex(city => city.GeonameId).IsUnique();

        builder.Property(city => city.Name).HasMaxLength(200);
        builder.Property(city => city.CountryCode).HasMaxLength(2).IsFixedLength();
        builder.Property(city => city.Admin1Code).HasMaxLength(20);
        builder.Property(city => city.Admin1Name).HasMaxLength(200);
        builder.Property(city => city.FeatureCode).HasMaxLength(10);
        builder.Property(city => city.Latitude);
        builder.Property(city => city.Longitude);
        builder.Property(city => city.Population);
        builder.Property(city => city.Timezone).HasMaxLength(40);

        // Spatial index IX_City_GeoPoint is created by raw SQL in the AddCity migration --
        // EF Core cannot declare spatial indexes.
        builder.Property(city => city.GeoPoint).HasColumnType("geography");

        builder.HasIndex(city => city.Population).IsDescending();
    }
}
