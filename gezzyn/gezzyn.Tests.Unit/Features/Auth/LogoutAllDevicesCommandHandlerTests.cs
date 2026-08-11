using FluentAssertions;
using gezzyn.Application.Features.Auth.Commands.LogoutAllDevices;
using gezzyn.Domain.Entities;
using gezzyn.Domain.Interfaces;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace gezzyn.Tests.Unit.Features.Auth
{
    public class LogoutAllDevicesCommandHandlerTests
    {
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
        private readonly Mock<IRepository<RefreshToken>> _refreshTokenRepoMock = new();
        private readonly LogoutAllDevicesCommandHandler _handler;

        public LogoutAllDevicesCommandHandlerTests()
        {
            _uowMock.Setup(u => u.Repository<RefreshToken>()) .Returns(_refreshTokenRepoMock.Object);
            _uowMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            _handler = new LogoutAllDevicesCommandHandler(_uowMock.Object, _currentUserServiceMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldRevokeAllActiveTokens_WhenUserHasMultipleDevices()
        {
            var userId = Guid.NewGuid();

            var token1 = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Token = "token_mobile",
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };
            var token2 = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Token = "token_web",
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };
            var token3 = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Token = "token_tablet",
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            _refreshTokenRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<RefreshToken, bool>>>()))
                                 .ReturnsAsync(new List<RefreshToken> { token1, token2, token3 });

            var cmd = new LogoutAllDevicesCommand();

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.Status.Should().Be("Success");

            token1.RevokedAt.Should().NotBeNull();
            token2.RevokedAt.Should().NotBeNull();
            token3.RevokedAt.Should().NotBeNull();

            _refreshTokenRepoMock.Verify(r => r.UpdateAsync(It.IsAny<RefreshToken>()), Times.Exactly(3));
            _uowMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldReturnSuccess_WhenNoActiveTokensExist()
        {
            _refreshTokenRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<RefreshToken, bool>>>()))
                .ReturnsAsync(new List<RefreshToken>());

            var cmd = new LogoutAllDevicesCommand();

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.Status.Should().Be("Success");
            _refreshTokenRepoMock.Verify(r => r.UpdateAsync(It.IsAny<RefreshToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldOnlyRevokeActiveTokens_WhenSomeAreAlreadyRevoked()
        {
            var userId = Guid.NewGuid();

            var activeToken1 = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Token = "active_1",
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };
            var activeToken2 = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Token = "active_2",
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };
            var alreadyRevokedToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Token = "already_revoked",
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                RevokedAt = DateTime.UtcNow.AddDays(-1) 
            };

            _refreshTokenRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<RefreshToken, bool>>>()))
                                 .ReturnsAsync(new List<RefreshToken> { activeToken1, activeToken2, alreadyRevokedToken } );

            var cmd = new LogoutAllDevicesCommand();

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.Status.Should().Be("Success");
            activeToken1.RevokedAt.Should().NotBeNull();
            activeToken1.RevokedAt.Should().NotBeNull();
            activeToken2.RevokedAt.Should().NotBeNull();

            _refreshTokenRepoMock.Verify(
                r => r.UpdateAsync(It.IsAny<RefreshToken>()), Times.Exactly(2));
        }
    }
}
