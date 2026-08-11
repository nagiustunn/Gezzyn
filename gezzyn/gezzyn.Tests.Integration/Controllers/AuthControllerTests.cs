using Docker.DotNet.Models;
using FluentAssertions;
using gezzyn.Application.DTO.Auth;
using gezzyn.Application.Features.Auth.Commands.RefreshToken;
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

        public async Task InitializeAsync() => await _db.ResetDatabaseAsync();
        public Task DisposeAsync() => Task.CompletedTask;

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
        public async Task Register_ShouldReturn400_WhenUserNameAlreadyExists()
        {
            var req = BuildRegisterRequest();
            await _client.PostAsJsonAsync("/api/auth/register", req);

            var req2 = BuildRegisterRequest();
            req2.UserName = req.UserName;

            var response = await _client.PostAsJsonAsync("/api/auth/register", req2);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var body = await response.Content.ReadFromJsonAsync<Response<AuthResponse>>();
            body!.Message.Should().Contain("zaten alınmış");
        }

        [Theory]
        [InlineData("", "Üstün", "user1", "test@test.com", "Test123!")]
        [InlineData("Nagihan", "", "user2", "test@test.com", "Test123!")]
        [InlineData("Nagihan", "Üstün", "", "test@test.com", "Test123!")]
        [InlineData("Nagihan", "Üstün", "user3", "gecersiz-email", "Test123!")]
        [InlineData("Nagihan", "Üstün", "user4", "test@test.com", "kisa")]
        public async Task Register_ShouldReturn400_WhenValidationFails(string name, string surname, string userName, string email, string password)
        {
            var req = new RegisterRequestDto
            {
                Name = name,
                Surname = surname,
                UserName = userName,
                Email = email,
                Password = password
            };

            var response = await _client.PostAsJsonAsync("/api/auth/register", req);

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
        public async Task Login_ShouldReturn200_WithValidCredentials()
        {
            var req = BuildRegisterRequest();
            await RegisterAndGetTokensAsync(req);

            var response = await _client.PostAsJsonAsync("/api/auth/login",
                new LoginRequest { Email = req.Email, Password = req.Password });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<Response<AuthResponseDto>>();
            body!.Data!.AccessToken.Should().NotBeNullOrEmpty();
            body.Data.User.Email.Should().Be(req.Email.ToLowerInvariant());
        }


        [Fact]
        public async Task Login_ShouldReturn401_WithWrongPassword()
        {
            var response = await _client.PostAsJsonAsync("/api/auth/login",
                new LoginRequest { Email = "yok@gezzyn.app", Password = "YanlisŞifre" });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Refresh_ShouldReturn200_WithValidRefreshToken()
        {
            var tokens = await RegisterAndGetTokensAsync();

            var response = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenCommand(tokens.RefreshToken));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<Response<AuthResponseDto>>();
            body!.Data!.AccessToken.Should().NotBeNullOrEmpty();
            body.Data.RefreshToken.Should().NotBeNullOrEmpty();

            body.Data.AccessToken.Should().NotBe(tokens.AccessToken);
            body.Data.RefreshToken.Should().NotBe(tokens.RefreshToken);
        }

        [Fact]
        public async Task Refresh_ShouldReturn401_WhenRefreshTokenIsInvalid()
        {
            var response = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenCommand("gecersiz_token_123"));

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Refresh_ShouldReturn401_WhenRefreshTokenUsedTwice()
        {
            var tokens = await RegisterAndGetTokensAsync();

            await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenCommand(tokens.RefreshToken));

            var response = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenCommand(tokens.RefreshToken));

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Logout_ShouldReturn200_AndInvalidateRefreshToken()
        {
            var tokens = await RegisterAndGetTokensAsync();
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokens.AccessToken);

            var logoutResponse = await _client.PostAsJsonAsync("/api/auth/logout", new RefreshTokenCommand(tokens.RefreshToken));

            logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var refreshResponse = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenCommand(tokens.RefreshToken));

            refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Logout_ShouldReturn401_WhenNotAuthenticated()
        {
            _client.DefaultRequestHeaders.Authorization = null;

            var response = await _client.PostAsJsonAsync("/api/auth/logout", new RefreshTokenCommand("herhangi_token"));

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task FullAuthFlow_Register_Login_Refresh_Logout()
        {
            var req = BuildRegisterRequest();
            var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", req);
            registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var registerBody = await registerResponse.Content.ReadFromJsonAsync<Response<AuthResponseDto>>();
            var firstTokens = registerBody!.Data!;

            var loginResponse = await _client.PostAsJsonAsync("/api/auth/login",
                new LoginRequest { Email = req.Email, Password = req.Password });
            loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var loginBody = await loginResponse.Content.ReadFromJsonAsync<Response<AuthResponseDto>>();
            var loginTokens = loginBody!.Data!;

            var refreshResponse = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenCommand(loginTokens.RefreshToken));
            refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var refreshBody = await refreshResponse.Content.ReadFromJsonAsync<Response<AuthResponseDto>>();
            var newTokens = refreshBody!.Data!;

            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", newTokens.AccessToken);
            var logoutResponse = await _client.PostAsJsonAsync("/api/auth/logout", new RefreshTokenCommand(newTokens.RefreshToken));
            logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var afterLogoutRefresh = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenCommand(newTokens.RefreshToken));
            afterLogoutRefresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #region Private Methods

        private RegisterRequestDto BuildRegisterRequest(string? email = null, string? userName = null)
        {
            var unique = Guid.NewGuid().ToString("N")[..8];
            return new RegisterRequestDto
            {
                Name = "Nagihan",
                Surname = "Üstün",
                UserName = userName ?? $"user_{unique}",
                Email = email ?? $"user_{unique}@gezzyn.app",
                Password = "Test123!"
            };
        }

        private async Task<AuthResponseDto> RegisterAndGetTokensAsync(RegisterRequestDto? req = null)
        {
            req ??= BuildRegisterRequest();
            var response = await _client.PostAsJsonAsync("/api/auth/register", req);
            var body = await response.Content.ReadFromJsonAsync<Response<AuthResponseDto>>();
            return body!.Data!;
        }

        #endregion
    }
}
