using Core.Data.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Core.Data.Config;

public class WeatherActivityConfig : IEntityTypeConfiguration<WeatherActivity>
{
    public void Configure(EntityTypeBuilder<WeatherActivity> builder)
    {
        builder.ToTable("WeatherActivity", "dbo");
        builder.HasKey(activity => activity.Id);
        builder.Property(activity => activity.Id).ValueGeneratedNever();

        builder.Property(activity => activity.SessionId).HasMaxLength(100);
        builder.Property(activity => activity.Feature).HasMaxLength(20).IsRequired();
        builder.Property(activity => activity.FeatureCategory).HasMaxLength(20).IsRequired();
        builder.Property(activity => activity.Host).HasMaxLength(10).IsRequired();
        builder.Property(activity => activity.Direction).HasMaxLength(10).IsRequired();
        builder.Property(activity => activity.Location).HasMaxLength(200);
        builder.Property(activity => activity.Content).HasColumnType("nvarchar(max)");
        builder.Property(activity => activity.ErrorMessage).HasColumnType("nvarchar(max)");

        builder.HasIndex(activity => activity.CorrelationId);
        builder.HasIndex(activity => activity.SessionId);
    }
}
