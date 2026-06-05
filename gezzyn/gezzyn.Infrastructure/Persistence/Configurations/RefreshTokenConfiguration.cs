using gezzyn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace gezzyn.Infrastructure.Persistence.Configurations
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.ToTable("refresh_tokens");
            builder.HasKey(r => r.Id);

            builder.Property(r => r.Token).IsRequired().HasMaxLength(256);
            builder.HasIndex(r => r.Token).IsUnique();

            builder.Ignore(r => r.IsExpired);
            builder.Ignore(r => r.IsRevoked);
            builder.Ignore(r => r.IsActive);
        }
    }
}
