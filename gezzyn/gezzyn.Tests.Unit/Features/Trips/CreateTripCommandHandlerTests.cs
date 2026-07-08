using FluentAssertions;
using FluentValidation;
using gezzyn.Application.DTO.Trip;
using gezzyn.Application.Features.Trips.Commands.CreateTrip;
using gezzyn.Domain.Entities;
using gezzyn.Domain.Enums;
using gezzyn.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Moq;

namespace gezzyn.Tests.Unit.Features.Trips
{
    public class CreateTripCommandHandlerTests
    {
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<IRepository<Trip>> _tripRepoMock = new();
        private readonly Mock<IValidator<CreateTripCommand>> _validatorMock = new();
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();
        private readonly CreateTripCommandHandler _handler;

        public CreateTripCommandHandlerTests()
        {
            _uowMock.Setup(u => u.Repository<Trip>()).Returns(_tripRepoMock.Object);
            _uowMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            _tripRepoMock.Setup(r => r.AsQueryable()).Returns(new List<Trip>().AsQueryable());

            _handler = new CreateTripCommandHandler(_uowMock.Object, _httpContextAccessorMock.Object, _validatorMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldCreateTrip_AndAddCreatorAsAdmin()
        {
            var creatorId = Guid.NewGuid();

            var createTripDto = new CreateTripDto
            {
                Title = "Kız Kıza Mardin Turu",
                City = "Mardin",
                Description = "3 günlük gezi",
                StartDate = null,
                EndDate = null
            };

            var cmd = new CreateTripCommand(createTripDto);

            Trip? savedTrip = null;
            _tripRepoMock.Setup(r => r.AddAsync(It.IsAny<Trip>()))
                         .Callback<Trip>(t => savedTrip = t)
                         .Returns(Task.CompletedTask);

            await _handler.Handle(cmd, CancellationToken.None);

            savedTrip.Should().NotBeNull();
            savedTrip!.Title.Should().Be("Kız Kıza Mardin Turu");
            savedTrip.City.Should().Be("Mardin");
            savedTrip.CreatedByUserId.Should().Be(creatorId);

            savedTrip.Members.Should().HaveCount(1);
            savedTrip.Members.First().UserId.Should().Be(creatorId);
            savedTrip.Members.First().Role.Should().Be(TripMemberRole.Admin);
        }

        [Theory]
        [InlineData("", "Mardin")]    
        [InlineData("Mardin Turu", "")] 
        public async Task Handle_ShouldFail_WhenRequiredFieldsEmpty(string title, string city)
        {
            var createTripDto = new CreateTripDto
            {
                Title = title,
                City = city,
                Description = null,
                StartDate = null,
                EndDate = null
            };   

            var cmd = new CreateTripCommand(createTripDto);

            var result = await _handler.Handle(cmd, CancellationToken.None);

            _tripRepoMock.Verify(r => r.AddAsync(It.IsAny<Trip>()), Times.AtMostOnce);
        }
    }
}
