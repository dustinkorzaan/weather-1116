using Core.Data.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Data.Config;

public class UserPinConfig : IEntityTypeConfiguration<UserPin>
{
    public void Configure(EntityTypeBuilder<UserPin> builder)
    {
        builder.ToTable("UserPin", "dbo");
        builder.HasKey(pin => pin.Id);
        builder.Property(pin => pin.Id).ValueGeneratedNever();

        builder.Property(pin => pin.UserId);
        builder.Property(pin => pin.Latitude);
        builder.Property(pin => pin.Longitude);
        builder.Property(pin => pin.LocationName).HasColumnType("nvarchar(max)");

        builder.HasOne(pin => pin.User)
            .WithMany(user => user.UserPins)
            .HasForeignKey(pin => pin.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
