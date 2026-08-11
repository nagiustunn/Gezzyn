using FluentAssertions;
using gezzyn.Application.DTO.Auth;
using gezzyn.Application.DTO.PlaceVisit;
using gezzyn.Application.DTO.Trip;
using gezzyn.Application.Features.Trips.Commands.CreateTrip;
using gezzyn.Domain.DTO;
using gezzyn.Domain.Entities;
using gezzyn.Domain.Enums;
using gezzyn.Infrastructure.Persistence;
using gezzyn.Tests.Integration.Common;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace gezzyn.Tests.Integration.Controllers
{
    [Collection("IntegrationTests")]
    public class TripsControllerTests : IClassFixture<GezzynWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly DatabaseFixture _db;
        private readonly GezzynWebApplicationFactory _factory;
        public TripsControllerTests(GezzynWebApplicationFactory factory, DatabaseFixture db)
        {
            _client = factory.CreateClient();
            _db = db;
            _factory = factory;
        }

        public async Task InitializeAsync() => await _db.ResetDatabaseAsync();
        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public async Task CreateTrip_ShouldReturn201_WhenAuthenticated()
        {
            var token = await GetTokenAsync();
            SetAuthHeader(token);

            var tripDto = new CreateTripDto
            {
                Title = "Kız Kıza Mardin Turu",
                City = "Mardin",
                Description = "3 günlük gezi planı"
            };

            var response = await _client.PostAsJsonAsync("/api/trips", tripDto);

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var body = await response.Content.ReadFromJsonAsync<Response<TripDto>>();
            body!.Data!.Title.Should().Be("Kız Kıza Mardin Turu");
            body.Data.City.Should().Be("Mardin");
            body.Data.Members.Should().HaveCount(1);
            body.Data.Members[0].Role.Should().Be("Admin");
        }

        [Fact]
        public async Task GetTrips_ShouldReturn401_WhenNotAuthenticated()
        {
            _client.DefaultRequestHeaders.Authorization = null;

            var response = await _client.GetAsync("/api/trips");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Theory]
        [InlineData("", "Mardin")]
        [InlineData("Mardin Turu", "")]
        public async Task CreateTrip_ShouldReturn400_WhenRequiredFieldsMissing(
        string title, string city)
        {
            var token = await GetTokenAsync();
            SetAuthHeader(token);

            var response = await _client.PostAsJsonAsync("/api/trips", new CreateTripDto { Title = title, City = city });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task GetTripById_ShouldReturn200_WhenUserIsMember()
        {
            var token = await GetTokenAsync();
            SetAuthHeader(token);

            var trip = await CreateTripAsync("Mardin Turu");

            var response = await _client.GetAsync($"/api/trips/{trip.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content.ReadFromJsonAsync<Response<TripDto>>();
            body!.Data!.Id.Should().Be(trip.Id);
            body.Data.Title.Should().Be("Mardin Turu");
        }

        [Fact]
        public async Task GetTripById_ShouldReturn404_WhenTripNotFound()
        {
            var token = await GetTokenAsync();
            SetAuthHeader(token);

            var response = await _client.GetAsync($"/api/trips/{Guid.NewGuid()}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetTripById_ShouldReturn401_WhenUserIsNotMember()
        {
            var tokenA = await GetTokenAsync();
            SetAuthHeader(tokenA);
            var trip = await CreateTripAsync();

            var tokenB = await GetTokenAsync();
            SetAuthHeader(tokenB);

            var response = await _client.GetAsync($"/api/trips/{trip.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetMyTrips_ShouldReturnOnlyUserTrips()
        {
            var tokenA = await GetTokenAsync();
            SetAuthHeader(tokenA);
            await CreateTripAsync("Mardin Turu");
            await CreateTripAsync("Kapadokya Turu");

            var tokenB = await GetTokenAsync();
            SetAuthHeader(tokenB);
            await CreateTripAsync("Efes Turu");

            SetAuthHeader(tokenA);
            var response = await _client.GetAsync("/api/trips");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<Response<List<TripDto>>>();
            body!.Data.Should().HaveCount(2);
            body.Data.Should().Contain(t => t.Title == "Mardin Turu");
            body.Data.Should().Contain(t => t.Title == "Kapadokya Turu");
            body.Data.Should().NotContain(t => t.Title == "Efes Turu");
        }

        [Fact]
        public async Task GetMyTrips_ShouldReturnEmpty_WhenNoTrips()
        {
            var token = await GetTokenAsync();
            SetAuthHeader(token);

            var response = await _client.GetAsync("/api/trips");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<Response<List<TripDto>>>();
            body!.Data.Should().BeEmpty();
        }

        [Fact]
        public async Task AddPlace_ShouldReturn200_WhenUserIsMemberAndPlaceExists()
        {
            var token = await GetTokenAsync();
            SetAuthHeader(token);

            var trip = await CreateTripAsync();

            var placeId = await SeedPlaceAsync();

            var response = await _client.PostAsJsonAsync($"/api/trips/{trip.Id}/places", placeId);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<Response<PlaceVisitDto>>();
            body!.Data!.PlaceId.Should().Be(placeId);
            body.Data.Order.Should().Be(1);
        }

        [Fact]
        public async Task AddPlace_ShouldReturn400_WhenPlaceAlreadyInTrip()
        {
            var token = await GetTokenAsync();
            SetAuthHeader(token);
            var trip = await CreateTripAsync();
            var placeId = await SeedPlaceAsync();

            await _client.PostAsJsonAsync($"/api/trips/{trip.Id}/places", placeId);

            var response = await _client.PostAsJsonAsync($"/api/trips/{trip.Id}/places", placeId);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var body = await response.Content.ReadFromJsonAsync<Response<PlaceVisitDto>>();
            body!.Message.Should().Contain("zaten listede");
        }

        [Fact]
        public async Task AddPlace_ShouldReturn401_WhenUserIsNotMember()
        {
            var tokenA = await GetTokenAsync();
            SetAuthHeader(tokenA);
            var trip = await CreateTripAsync();
            var placeId = await SeedPlaceAsync();

            var tokenB = await GetTokenAsync();
            SetAuthHeader(tokenB);

            var response = await _client.PostAsJsonAsync($"/api/trips/{trip.Id}/places", placeId);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ReorderPlaces_ShouldReturn200_AndUpdateOrder()
        {
            var token = await GetTokenAsync();
            SetAuthHeader(token);
            var trip = await CreateTripAsync();

            var placeId1 = await SeedPlaceAsync("Deyrulzafaran Manastırı");
            var placeId2 = await SeedPlaceAsync("Kasımiye Medresesi");
            var placeId3 = await SeedPlaceAsync("Eski Mardin Çarşısı");

            await _client.PostAsJsonAsync($"/api/trips/{trip.Id}/places", placeId1);
            await _client.PostAsJsonAsync($"/api/trips/{trip.Id}/places", placeId2);
            await _client.PostAsJsonAsync($"/api/trips/{trip.Id}/places", placeId3);

            var response = await _client.PutAsJsonAsync($"/api/trips/{trip.Id}/reorder", new List<Guid> { placeId3, placeId1, placeId2 });

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var tripResponse = await _client.GetAsync($"/api/trips/{trip.Id}");
            var tripBody = await tripResponse.Content.ReadFromJsonAsync<Response<TripDto>>();
            var visits = tripBody!.Data!.PlaceVisits.OrderBy(pv => pv.Order).ToList();

            visits[0].PlaceId.Should().Be(placeId3);
            visits[1].PlaceId.Should().Be(placeId1);
            visits[2].PlaceId.Should().Be(placeId2);
        }

        [Fact]
        public async Task ReorderPlaces_ShouldReturn401_WhenUserIsNotMember()
        {
            var tokenA = await GetTokenAsync();
            SetAuthHeader(tokenA);
            var trip = await CreateTripAsync();

            var tokenB = await GetTokenAsync();
            SetAuthHeader(tokenB);

            var response = await _client.PutAsJsonAsync($"/api/trips/{trip.Id}/reorder", new List<Guid> { Guid.NewGuid() });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task FullTripFlow_Create_AddPlaces_Reorder_GetById()
        {
            var token = await GetTokenAsync();
            SetAuthHeader(token);

            var createDto = new CreateTripDto
            {
                Title = "Mardin Turu",
                City = "Mardin",
            };

            var createResponse = await _client.PostAsJsonAsync("/api/trips", createDto);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            var trip = (await createResponse.Content.ReadFromJsonAsync<Response<TripDto>>())!.Data!;

            var placeId1 = await SeedPlaceAsync("Deyrulzafaran Manastırı");
            var placeId2 = await SeedPlaceAsync("Kasımiye Medresesi");
            var placeId3 = await SeedPlaceAsync("Mardin Müzesi");

            var add1 = await _client.PostAsJsonAsync($"/api/trips/{trip.Id}/places", placeId1);
            add1.StatusCode.Should().Be(HttpStatusCode.OK);

            var add2 = await _client.PostAsJsonAsync($"/api/trips/{trip.Id}/places", placeId2);
            add2.StatusCode.Should().Be(HttpStatusCode.OK);

            var add3 = await _client.PostAsJsonAsync($"/api/trips/{trip.Id}/places", placeId3);
            add3.StatusCode.Should().Be(HttpStatusCode.OK);

            var getResponse = await _client.GetAsync($"/api/trips/{trip.Id}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var getBody = (await getResponse.Content.ReadFromJsonAsync<Response<TripDto>>())!.Data!;
            getBody.PlaceCount.Should().Be(3);
            getBody.PlaceVisits.Should().BeInAscendingOrder(pv => pv.Order);

            var reorderResponse = await _client.PutAsJsonAsync($"/api/trips/{trip.Id}/reorder", new List<Guid> { placeId3, placeId2, placeId1 });
            reorderResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var finalResponse = await _client.GetAsync($"/api/trips/{trip.Id}");
            var finalBody = (await finalResponse.Content.ReadFromJsonAsync<Response<TripDto>>())!.Data!;
            var ordered = finalBody.PlaceVisits.OrderBy(pv => pv.Order).ToList();

            ordered[0].PlaceId.Should().Be(placeId3);
            ordered[1].PlaceId.Should().Be(placeId2);
            ordered[2].PlaceId.Should().Be(placeId1);
        }

        #region Private Methods

        private async Task<string> GetTokenAsync()
        {
            var unique = Guid.NewGuid().ToString("N")[..8];
            var registerReq = new RegisterRequestDto
            {
                Name = "Test",
                Surname = "User",
                UserName = $"user_{unique}",
                Email = $"user_{unique}@gezzyn.app",
                Password = "Test123!"
            };

            var response = await _client.PostAsJsonAsync("/api/auth/register", registerReq);
            var body = await response.Content.ReadFromJsonAsync<Response<AuthResponseDto>>();
            return body!.Data!.AccessToken;
        }

        private void SetAuthHeader(string token)
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        private async Task<TripDto> CreateTripAsync(string title = "Mardin Turu", string city = "Mardin")
        {
            var dto = new CreateTripDto
            {
                Title = title,
                City = city,
                Description = "Test gezisi"
            };

            var response = await _client.PostAsJsonAsync("/api/trips", new CreateTripCommand(dto));

            var body = await response.Content.ReadFromJsonAsync<Response<TripDto>>();
            return body!.Data!;
        }

        private async Task<Guid> SeedPlaceAsync(string name = "Test Mekan")
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var place = new Place
            {
                Id = Guid.NewGuid(),
                Name = name,
                City = "Mardin",
                Latitude = 37.2940 + Random.Shared.NextDouble() * 0.01,
                Longitude = 40.7830 + Random.Shared.NextDouble() * 0.01,
                Category = PlaceCategory.HistoricalSite,
                Source = PlaceSource.Manual,
                Country = "TR"
            };

            db.Places.Add(place);
            await db.SaveChangesAsync();

            return place.Id;
        }

        #endregion
    }
}