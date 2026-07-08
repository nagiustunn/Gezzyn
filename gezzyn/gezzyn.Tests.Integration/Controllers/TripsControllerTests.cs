using FluentAssertions;
using gezzyn.Application.DTO.Auth;
using gezzyn.Application.DTO.Trip;
using gezzyn.Domain.DTO;
using gezzyn.Tests.Integration.Common;
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

        public TripsControllerTests(GezzynWebApplicationFactory factory, DatabaseFixture db)
        {
            _client = factory.CreateClient();
            _db = db;
        }

        private async Task<string> GetTokenAsync()
        {
            var email = $"trip_test_{Guid.NewGuid():N}@gezzyn.app";
            var registerReq = new RegisterRequestDto
            {
                Name = "Test",
                Surname = "User",
                UserName = $"user_{Guid.NewGuid():N}",
                Email = email,
                Password = "Test123!"
            };

            var response = await _client.PostAsJsonAsync("/api/auth/register", registerReq);
            var body = await response.Content.ReadFromJsonAsync<Response<AuthResponseDto>>();
            return body!.Data!.AccessToken;
        }

        [Fact]
        public async Task CreateTrip_ShouldReturn201_WhenAuthenticated()
        {
            var token = await GetTokenAsync();
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

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

        [Fact]
        public async Task GetTripById_ShouldReturn404_WhenTripNotFound()
        {
            var token = await GetTokenAsync();
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.GetAsync($"/api/trips/{Guid.NewGuid()}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}