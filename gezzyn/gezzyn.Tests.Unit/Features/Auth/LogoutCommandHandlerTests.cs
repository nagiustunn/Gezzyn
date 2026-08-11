using FluentAssertions;
using gezzyn.Application.Features.Auth.Commands.Logout;
using gezzyn.Domain.Entities;
using gezzyn.Domain.Interfaces;
using Moq;
using System.Net;

namespace gezzyn.Tests.Unit.Features.Auth
{
    public class LogoutCommandHandlerTests
    {
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<IRepository<RefreshToken>> _refreshTokenRepoMock = new();
        private readonly LogoutCommandHandler _handler;

        public LogoutCommandHandlerTests()
        {
            _uowMock.Setup(u => u.Repository<RefreshToken>()).Returns(_refreshTokenRepoMock.Object);
            _uowMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            _handler = new LogoutCommandHandler(_uowMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldRevokeToken_WhenTokenIsActive()
        {
            var activeToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Token = "active_token",
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            _refreshTokenRepoMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<RefreshToken, bool>>>()))
                                 .ReturnsAsync(activeToken);

            var cmd = new LogoutCommand("active_token");

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.Status.Should().Be("Success");

            activeToken.RevokedAt.Should().NotBeNull();

            _refreshTokenRepoMock.Verify(r => r.UpdateAsync(It.IsAny<RefreshToken>()), Times.Once);

            _uowMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldReturnSuccess_WhenTokenAlreadyRevoked()
        {
            var revokedToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Token = "already_revoked_token",
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                RevokedAt = DateTime.UtcNow.AddHours(-1)
            };

            _refreshTokenRepoMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<RefreshToken, bool>>>()))
                                 .ReturnsAsync(revokedToken);

            var cmd = new LogoutCommand("already_revoked_token");

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.Status.Should().Be("Success");
            result.Message.Should().Contain("Zaten çıkış yapılmış");

            _refreshTokenRepoMock.Verify(r => r.UpdateAsync(It.IsAny<RefreshToken>()), Times.Never);
            _uowMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldReturnNotFound_WhenTokenDoesNotExist()
        {
            _refreshTokenRepoMock
                .Setup(r => r.FirstOrDefaultAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<RefreshToken, bool>>>()))
                .ReturnsAsync((RefreshToken?)null);

            var cmd = new LogoutCommand("olmayan_token");

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.Status.Should().Be("Error");
            result.StatusCode.Should().Be(HttpStatusCode.NotFound);
            _uowMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldReturnSuccess_WhenTokenIsExpiredButNotRevoked()
        {
            var expiredToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Token = "expired_token",
                ExpiresAt = DateTime.UtcNow.AddDays(-1)
            };

            _refreshTokenRepoMock
                .Setup(r => r.FirstOrDefaultAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<RefreshToken, bool>>>()))
                .ReturnsAsync(expiredToken);

            var cmd = new LogoutCommand("expired_token");

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.Status.Should().Be("Success");
        }
    }
}
