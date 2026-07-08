using FluentAssertions;
using gezzyn.Application.DTO.Auth;
using gezzyn.Domain.DTO;
using gezzyn.Tests.Integration.Common;
using Microsoft.AspNetCore.Identity.Data;
using System.Net;
using System.Net.Http.Json;

namespace gezzyn.Tests.Integration.Controllers
{
    [Collection("IntegrationTests")]
    public class AuthControllerTests : IClassFixture<GezzynWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly DatabaseFixture _db;

        public AuthControllerTests(GezzynWebApplicationFactory factory, DatabaseFixture db)
        {
            _client = factory.CreateClient();
            _db = db;
        }

        public async Task InitializeAsync()
        {
            await _db.ResetDatabaseAsync();
        }

        [Fact]
        public async Task Register_ShouldReturn200_WhenValidData()
        {
            var request = new RegisterRequestDto
            {
                Name = "Nagihan",
                Surname = "Üstün",
                UserName = $"nagihan_{Guid.NewGuid():N}", 
                Email = $"nagihan_{Guid.NewGuid():N}@gezzyn.app",
                Password = "Test123!"
            };

            var response = await _client.PostAsJsonAsync("/api/auth/register", request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content.ReadFromJsonAsync<Response<AuthResponseDto>>();
            body.Should().NotBeNull();
            body!.Data.Should().NotBeNull();
            body.Data!.AccessToken.Should().NotBeNullOrEmpty();
            body.Data.RefreshToken.Should().NotBeNullOrEmpty();
            body.Data.User.Email.Should().Be(request.Email.ToLowerInvariant());
        }

        [Fact]
        public async Task Register_ShouldReturn400_WhenEmailAlreadyExists()
        {
            var email = $"duplicate_{Guid.NewGuid():N}@gezzyn.app";
            var request = new RegisterRequestDto
            {
                Name = "Test",
                Surname = "User",
                UserName = $"user_{Guid.NewGuid():N}",
                Email = email,
                Password = "Test123!"
            };

            await _client.PostAsJsonAsync("/api/auth/register", request);

            request.UserName = $"user_{Guid.NewGuid():N}"; 

            var response = await _client.PostAsJsonAsync("/api/auth/register", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Login_ShouldReturn200_AfterRegister()
        {
            var email = $"login_test_{Guid.NewGuid():N}@gezzyn.app";

            await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto
            {
                Name = "Test",
                Surname = "User",
                UserName = $"user_{Guid.NewGuid():N}",
                Email = email,
                Password = "Test123!"
            });

            var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = "Test123!" });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<Response<AuthResponseDto>>();
            body!.Data!.AccessToken.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task Login_ShouldReturn401_WithWrongPassword()
        {
            var response = await _client.PostAsJsonAsync("/api/auth/login",
                new LoginRequest { Email = "yok@gezzyn.app", Password = "YanlisŞifre" });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}
