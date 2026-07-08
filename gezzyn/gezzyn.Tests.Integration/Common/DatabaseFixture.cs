using gezzyn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace gezzyn.Tests.Integration.Common
{
    public class DatabaseFixture : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
                                                          .WithImage("postgres:16-alpine")
                                                          .WithDatabase("gezzyn_test")
                                                          .WithUsername("postgres")
                                                          .WithPassword("test_password")
                                                          .Build();

        public string ConnectionString => _container.GetConnectionString();

        public async Task InitializeAsync()
        {
            await _container.StartAsync();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                          .UseNpgsql(ConnectionString)
                          .Options;

            await using var db = new ApplicationDbContext(options);
            await db.Database.MigrateAsync();
        }

        /// <summary>
        /// Her testten sonra tabloları temizler — testler birbirini etkilemez.
        /// </summary>
        public async Task ResetDatabaseAsync()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(ConnectionString)
                .Options;

            await using var db = new ApplicationDbContext(options);

            db.PlaceVisits.RemoveRange(db.PlaceVisits);
            db.TripMembers.RemoveRange(db.TripMembers);
            db.Trips.RemoveRange(db.Trips);
            db.RefreshTokens.RemoveRange(db.RefreshTokens);
            db.Places.RemoveRange(db.Places);
            db.Users.RemoveRange(db.Users);
            await db.SaveChangesAsync();
        }

        public async Task DisposeAsync()
        {
            await _container.StopAsync();
        }
    }
}
