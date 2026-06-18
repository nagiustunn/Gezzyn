using gezzyn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace gezzyn.Infrastructure.Persistence.Configurations
{
    public class TripMemberConfiguration : IEntityTypeConfiguration<TripMember>
    {
        public void Configure(EntityTypeBuilder<TripMember> builder)
        {
            builder.ToTable("trip_members");
            builder.HasKey(m => m.Id);

            builder.Property(m => m.Role).HasConversion<string>().HasMaxLength(20);

            builder.HasIndex(m => new { m.TripId, m.UserId }).IsUnique();

            builder.Navigation(x => x.User).AutoInclude();
        }
    }
}
