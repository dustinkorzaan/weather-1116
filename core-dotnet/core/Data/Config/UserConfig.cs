using Core.Data.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Data.Config;

public class UserConfig : IEntityTypeConfiguration<Domain.User>
{
    public void Configure(EntityTypeBuilder<Domain.User> builder)
    {
        builder.ToTable("User", "dbo");
        builder.HasKey(user => user.Id);
        builder.Property(user => user.Id).ValueGeneratedNever();

        builder.Property(user => user.FirstName).HasColumnType("nvarchar(max)");
        builder.Property(user => user.LastName).HasColumnType("nvarchar(max)");
        builder.Property(user => user.Email).HasColumnType("nvarchar(max)");

        builder.HasMany(user => user.UserPins)
            .WithOne(pin => pin.User)
            .HasForeignKey(pin => pin.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
