using gezzyn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace gezzyn.Infrastructure.Persistence
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<Trip> Trips => Set<Trip>();
        public DbSet<TripMember> TripMembers => Set<TripMember>();
        public DbSet<Place> Places => Set<Place>();
        public DbSet<PlaceVisit> PlaceVisits => Set<PlaceVisit>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            var dateTimeConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
               v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
               v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

            var nullableDateTimeConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime?, DateTime?>(
                v => !v.HasValue ? v : (v.Value.Kind == DateTimeKind.Utc ? v : v.Value.ToUniversalTime()),
                v => !v.HasValue ? v : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc));

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime))
                    {
                        property.SetValueConverter(dateTimeConverter);
                    }
                    else if (property.ClrType == typeof(DateTime?))
                    {
                        property.SetValueConverter(nullableDateTimeConverter);
                    }
                }
            }

            modelBuilder.Entity<User>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<Trip>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<Place>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<TripMember>().HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<PlaceVisit>().HasQueryFilter(x => !x.IsDeleted);
        }
    }
}
