using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using gezzyn.Application.Features.Auth.Commands.Register;
using gezzyn.Domain.Entities;
using gezzyn.Domain.Interfaces;
using Moq;
using System.Net;

namespace gezzyn.Tests.Unit.Features
{
    public class RegisterCommandHandlerTests
    {
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<IPasswordService> _passwordServiceMock = new();
        private readonly Mock<ITokenService> _tokenServiceMock = new();
        private readonly Mock<IValidator<RegisterCommand>> _validatorMock = new();
        private readonly Mock<IRepository<User>> _userRepoMock = new();
        private readonly Mock<IRepository<RefreshToken>> _refreshTokenRepoMock = new();

        private readonly RegisterCommandHandler _handler;

        public RegisterCommandHandlerTests()
        {
            _uowMock.Setup(u => u.Repository<User>()).Returns(_userRepoMock.Object);
            _uowMock.Setup(u => u.Repository<RefreshToken>()).Returns(_refreshTokenRepoMock.Object);
            _uowMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            _validatorMock.Setup(v => v.ValidateAsync(It.IsAny<RegisterCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(new ValidationResult());

            _handler = new RegisterCommandHandler(_uowMock.Object, _passwordServiceMock.Object, _tokenServiceMock.Object, _validatorMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldReturnSuccess_WhenEmailAndUserNameAreUnique()
        {
            var cmd = new RegisterCommand("Nagihan", "Üstün", "nagihan", "nagihan@gezzyn.app", "Test123!");

            _userRepoMock.Setup(r => r.AnyAsync(u => u.Email == cmd.Email.ToLowerInvariant())).ReturnsAsync(false);
            _userRepoMock.Setup(r => r.AnyAsync(u => u.UserName == cmd.UserName)).ReturnsAsync(false);

            _passwordServiceMock.Setup(p => p.Hash(cmd.Password)).Returns("hashed_password");
            _tokenServiceMock.Setup(t => t.GenerateToken(It.IsAny<User>())).Returns("access_token");
            _tokenServiceMock.Setup(t => t.GenerateRefreshToken()).Returns("refresh_token");

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.Should().NotBeNull();
            result.Status.Should().Be("Success");
            result.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Data.Should().NotBeNull();
            result.Data!.AccessToken.Should().Be("access_token");
            result.Data.User.Email.Should().Be("nagihan@gezzyn.app");

            _userRepoMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
            _refreshTokenRepoMock.Verify(r => r.AddAsync(It.IsAny<RefreshToken>()), Times.Once);
            _uowMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldReturnError_WhenEmailAlreadyExists()
        {
            var cmd = new RegisterCommand("Nagihan", "Üstün", "nagihan", "mevcut@gezzyn.app", "Test123!");

            _userRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>())).ReturnsAsync(true);

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.Status.Should().Be("Error");
            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            result.Message.Should().Contain("zaten kullanılıyor");

            _userRepoMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
            _uowMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        }
    }
}
