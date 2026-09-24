using Core.Data.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Data.Config;

public class UserCityConfig : IEntityTypeConfiguration<UserCity>
{
    public void Configure(EntityTypeBuilder<UserCity> builder)
    {
        builder.ToTable("UserCities", "dbo");
        builder.HasKey(city => city.Id);
        builder.Property(city => city.Id).ValueGeneratedNever();

        builder.Property(city => city.UserId);
        builder.Property(city => city.Latitude);
        builder.Property(city => city.Longitude);
        builder.Property(city => city.LocationName).HasColumnType("nvarchar(max)");

        builder.HasOne(city => city.User)
            .WithMany(user => user.UserCities)
            .HasForeignKey(city => city.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
