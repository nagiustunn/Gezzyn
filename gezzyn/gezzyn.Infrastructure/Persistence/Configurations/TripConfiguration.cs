using gezzyn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace gezzyn.Infrastructure.Persistence.Configurations
{
    public class TripConfiguration : IEntityTypeConfiguration<Trip>
    {
        public void Configure(EntityTypeBuilder<Trip> builder)
        {
            builder.ToTable("trips");
            builder.HasKey(t => t.Id);

            builder.Property(t => t.Title).IsRequired().HasMaxLength(200);
            builder.Property(t => t.City).IsRequired().HasMaxLength(100);
            builder.Property(t => t.Description).HasMaxLength(1000);
            builder.Property(t => t.CoverImageUrl).HasMaxLength(500);
            builder.Property(t => t.InviteCode).IsRequired().HasMaxLength(8);
            builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);

            builder.HasIndex(t => t.InviteCode).IsUnique();

            builder.HasOne(t => t.CreatedBy)
                .WithMany()
                .HasForeignKey(t => t.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(t => t.Members)
                .WithOne(m => m.Trip)
                .HasForeignKey(m => m.TripId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(t => t.PlaceVisits)
                .WithOne(pv => pv.Trip)
                .HasForeignKey(pv => pv.TripId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
