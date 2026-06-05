using gezzyn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace gezzyn.Infrastructure.Persistence.Configurations
{
    public class PlaceConfiguration : IEntityTypeConfiguration<Place>
    {
        public void Configure(EntityTypeBuilder<Place> builder)
        {
            builder.ToTable("places");
            builder.HasKey(p => p.Id);

            builder.Property(p => p.Name).IsRequired().HasMaxLength(300);
            builder.Property(p => p.Description).HasMaxLength(2000);
            builder.Property(p => p.FormattedAddress).HasMaxLength(500);
            builder.Property(p => p.City).HasMaxLength(100);
            builder.Property(p => p.District).HasMaxLength(100);
            builder.Property(p => p.Country).HasMaxLength(2).HasDefaultValue("TR");
            builder.Property(p => p.GooglePlaceId).HasMaxLength(200);
            builder.Property(p => p.GoogleMapsUrl).HasMaxLength(500);
            builder.Property(p => p.PrimaryPhotoUrl).HasMaxLength(500);
            builder.Property(p => p.EntranceFeeAmount).HasPrecision(10, 2);
            builder.Property(p => p.EntranceFeeNote).HasMaxLength(300);
            builder.Property(p => p.Category).HasConversion<string>().HasMaxLength(30);
            builder.Property(p => p.Source).HasConversion<string>().HasMaxLength(20);

            builder.Property(p => p.OpeningHoursJson).HasColumnType("jsonb");

            builder.HasIndex(p => p.GooglePlaceId)
                .IsUnique()
                .HasFilter("google_place_id IS NOT NULL");
        }
    }
}
