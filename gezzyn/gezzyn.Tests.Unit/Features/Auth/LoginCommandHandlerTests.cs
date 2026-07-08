using FluentAssertions;
using FluentValidation;
using gezzyn.Application.Features.Auth.Commands.Login;
using gezzyn.Domain.Entities;
using gezzyn.Domain.Interfaces;
using gezzyn.Tests.Unit.Common.Builders;
using Moq;
using System.Net;

namespace gezzyn.Tests.Unit.Features.Auth
{
    public class LoginCommandHandlerTests
    {
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<IPasswordService> _passwordServiceMock = new();
        private readonly Mock<ITokenService> _tokenServiceMock = new();
        private readonly Mock<IValidator<LoginCommand>> _validatorMock = new();
        private readonly Mock<IRepository<User>> _userRepoMock = new();
        private readonly Mock<IRepository<RefreshToken>> _refreshTokenRepoMock = new();
        private readonly LoginCommandHandler _handler;

        public LoginCommandHandlerTests()
        {
            _uowMock.Setup(u => u.Repository<User>()).Returns(_userRepoMock.Object);
            _uowMock.Setup(u => u.Repository<RefreshToken>()).Returns(_refreshTokenRepoMock.Object);
            _uowMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            _handler = new LoginCommandHandler(_uowMock.Object, _passwordServiceMock.Object, _tokenServiceMock.Object, _validatorMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldReturnSuccess_WhenCredentialsAreValid()
        {
            // Arrange
            var user = new UserBuilder()
                           .WithEmail("nagihan@gezzyn.app")
                           .WithPasswordHash("hashed_password")
                           .Build();

            var cmd = new LoginCommand("nagihan@gezzyn.app", "Test123!");

            _userRepoMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>())).ReturnsAsync(user);

            _passwordServiceMock.Setup(p => p.Verify("Test123!", "hashed_password")).Returns(true);

            _tokenServiceMock.Setup(t => t.GenerateToken(user)).Returns("access_token");
            _tokenServiceMock.Setup(t => t.GenerateRefreshToken()).Returns("refresh_token");

            // Act
            var result = await _handler.Handle(cmd, CancellationToken.None);

            // Assert
            result.Status.Should().Be("Success");
            result.Data!.AccessToken.Should().Be("access_token");
            result.Data.User.Email.Should().Be("nagihan@gezzyn.app");
        }

        [Fact]
        public async Task Handle_ShouldReturnUnauthorized_WhenUserNotFound()
        {
            // Arrange
            _userRepoMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>())).ReturnsAsync((User?)null);

            var cmd = new LoginCommand("yok@gezzyn.app", "Test123!");

            // Act
            var result = await _handler.Handle(cmd, CancellationToken.None);

            // Assert
            result.Status.Should().Be("Error");
            result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            result.Data.Should().BeNull();
        }

        [Fact]
        public async Task Handle_ShouldReturnUnauthorized_WhenPasswordIsWrong()
        {
            // Arrange
            var user = new UserBuilder().WithPasswordHash("hashed_password").Build();

            _userRepoMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>())).ReturnsAsync(user);

            _passwordServiceMock.Setup(p => p.Verify("YanlisŞifre", "hashed_password")).Returns(false);

            var cmd = new LoginCommand("nagihan@gezzyn.app", "YanlisŞifre");

            // Act
            var result = await _handler.Handle(cmd, CancellationToken.None);

            // Assert
            result.Status.Should().Be("Error");
            result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}
