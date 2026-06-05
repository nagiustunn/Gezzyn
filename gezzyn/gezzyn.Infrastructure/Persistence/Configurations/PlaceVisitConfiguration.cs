using gezzyn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace gezzyn.Infrastructure.Persistence.Configurations
{
    public class PlaceVisitConfiguration : IEntityTypeConfiguration<PlaceVisit>
    {
        public void Configure(EntityTypeBuilder<PlaceVisit> builder)
        {
            builder.ToTable("place_visits");
            builder.HasKey(pv => pv.Id);

            builder.Property(pv => pv.Note).HasMaxLength(500);
            builder.Property(pv => pv.PlannedArrivalTime).HasMaxLength(5); 
            builder.Property(pv => pv.Status).HasConversion<string>().HasMaxLength(20);

            builder.HasIndex(pv => new { pv.TripId, pv.PlaceId }).IsUnique();

            builder.HasOne(pv => pv.Place)
                .WithMany(p => p.TripVisits)
                .HasForeignKey(pv => pv.PlaceId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(pv => pv.AddedBy)
                .WithMany()
                .HasForeignKey(pv => pv.AddedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
