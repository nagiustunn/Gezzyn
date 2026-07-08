using FluentAssertions;
using gezzyn.Application.Features.Auth.Commands.RefreshToken;
using gezzyn.Domain.Entities;
using gezzyn.Domain.Interfaces;
using gezzyn.Tests.Unit.Common.Builders;
using Moq;
using System.Net;

namespace gezzyn.Tests.Unit.Features.Auth
{
    public class RefreshTokenCommandHandlerTests
    {
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<ITokenService> _tokenServiceMock = new();
        private readonly Mock<IRepository<User>> _userRepoMock = new();
        private readonly Mock<IRepository<RefreshToken>> _refreshTokenRepoMock = new();
        private readonly RefreshTokenCommandHandler _handler;

        public RefreshTokenCommandHandlerTests()
        {
            _uowMock.Setup(u => u.Repository<User>()).Returns(_userRepoMock.Object);
            _uowMock.Setup(u => u.Repository<RefreshToken>()).Returns(_refreshTokenRepoMock.Object);
            _uowMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            _handler = new RefreshTokenCommandHandler(_uowMock.Object, _tokenServiceMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldReturnNewTokens_WhenRefreshTokenIsActive()
        {
            var user = new UserBuilder().Build();
            var activeToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = "valid_refresh_token",
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            _refreshTokenRepoMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<RefreshToken, bool>>>()))
                                 .ReturnsAsync(activeToken);

            _userRepoMock.Setup(r => r.GetByIdAsync(user.Id))
                         .ReturnsAsync(user);

            _tokenServiceMock.Setup(t => t.GenerateToken(user)).Returns("new_access_token");
            _tokenServiceMock.Setup(t => t.GenerateRefreshToken()).Returns("new_refresh_token");

            var cmd = new RefreshTokenCommand("valid_refresh_token");

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.Status.Should().Be("Success");
            result.Data!.AccessToken.Should().Be("new_access_token");
            result.Data.RefreshToken.Should().Be("new_refresh_token");

            activeToken.RevokedAt.Should().NotBeNull();

            _refreshTokenRepoMock.Verify(r => r.AddAsync(It.IsAny<RefreshToken>()), Times.Once);
            _uowMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldReturnUnauthorized_WhenTokenNotFound()
        {
            _refreshTokenRepoMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<RefreshToken, bool>>>()))
                                 .ReturnsAsync((RefreshToken?)null);

            var cmd = new RefreshTokenCommand("gecersiz_token");

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.Status.Should().Be("Error");
            result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            _refreshTokenRepoMock.Verify(r => r.AddAsync(It.IsAny<RefreshToken>()), Times.Never);
            _uowMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldReturnUnauthorized_WhenTokenIsExpired()
        {
            var expiredToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Token = "expired_token",
                ExpiresAt = DateTime.UtcNow.AddDays(-1) 
            };

            _refreshTokenRepoMock
                .Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<RefreshToken, bool>>>()))
                .ReturnsAsync(expiredToken);

            var cmd = new RefreshTokenCommand("expired_token");

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.Status.Should().Be("Error");
            result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            _refreshTokenRepoMock.Verify(r => r.AddAsync(It.IsAny<RefreshToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldReturnUnauthorized_WhenTokenIsRevoked()
        {
            var revokedToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Token = "revoked_token",
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                RevokedAt = DateTime.UtcNow.AddHours(-2) 
            };

            _refreshTokenRepoMock
                .Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<RefreshToken, bool>>>()))
                .ReturnsAsync(revokedToken);

            var cmd = new RefreshTokenCommand("revoked_token");

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.Status.Should().Be("Error");
            result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Handle_ShouldReturnUnauthorized_WhenUserNotFound()
        {
            var activeToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Token = "orphan_token",
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            _refreshTokenRepoMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<RefreshToken, bool>>>()))
                                 .ReturnsAsync(activeToken);

            _userRepoMock.Setup(r => r.GetByIdAsync(activeToken.UserId))
                         .ReturnsAsync((User?)null);

            var cmd = new RefreshTokenCommand("orphan_token");

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.Status.Should().Be("Error");
            result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            _refreshTokenRepoMock.Verify(r => r.AddAsync(It.IsAny<RefreshToken>()), Times.Never);
        }
    }
}
