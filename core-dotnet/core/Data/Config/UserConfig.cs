using Core.Data.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Data.Config;

public class UserConfig : IEntityTypeConfiguration<Core.Data.Domain.User>
{
    public void Configure(EntityTypeBuilder<Core.Data.Domain.User> builder)
    {
        builder.ToTable("Users", "dbo");
        builder.HasKey(user => user.Id);
        builder.Property(user => user.Id).ValueGeneratedNever();

        builder.Property(user => user.FirstName).HasColumnType("nvarchar(max)");
        builder.Property(user => user.LastName).HasColumnType("nvarchar(max)");
        builder.Property(user => user.Email).HasColumnType("nvarchar(max)");

        builder.HasMany(user => user.UserCities)
            .WithOne(city => city.User)
            .HasForeignKey(city => city.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
