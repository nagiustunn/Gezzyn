using FluentAssertions;
using FluentValidation;
using gezzyn.Application.Features.Trips.Commands.AddPlaceToTrip;
using gezzyn.Domain.Entities;
using gezzyn.Domain.Interfaces;
using gezzyn.Tests.Unit.Common.Builders;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Net;

namespace gezzyn.Tests.Unit.Features.Trips
{
    public class AddPlaceToTripCommandHandlerTests
    {
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<IRepository<Trip>> _tripRepoMock = new();
        private readonly Mock<IRepository<Place>> _placeRepoMock = new();
        private readonly Mock<IRepository<PlaceVisit>> _visitRepoMock = new();
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();
        private readonly Mock<IValidator<AddPlaceToTripCommand>> _validatorMock = new();
        private readonly Mock<ITripNotificationService> _notificationMock = new();
        private readonly AddPlaceToTripCommandHandler _handler;

        public AddPlaceToTripCommandHandlerTests()
        {
            _uowMock.Setup(u => u.Repository<Trip>()).Returns(_tripRepoMock.Object);
            _uowMock.Setup(u => u.Repository<Place>()).Returns(_placeRepoMock.Object);
            _uowMock.Setup(u => u.Repository<PlaceVisit>()).Returns(_visitRepoMock.Object);
            _uowMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            _notificationMock.Setup(n => n.NotifyPlaceAdded(It.IsAny<Guid>(), It.IsAny<object>())).Returns(Task.CompletedTask);

            _handler = new AddPlaceToTripCommandHandler(_uowMock.Object, _httpContextAccessorMock.Object, _notificationMock.Object, _validatorMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldAddPlace_WhenUserIsMember()
        {
            var userId = Guid.NewGuid();
            var trip = new TripBuilder().WithCreator(userId).Build();
            var place = new PlaceBuilder().Build();

            _tripRepoMock.Setup(r => r.AsQueryable()).Returns(new List<Trip> { trip }.AsQueryable());
            _placeRepoMock.Setup(r => r.GetByIdAsync(place.Id)).ReturnsAsync(place);

            var cmd = new AddPlaceToTripCommand(trip.Id, place.Id);

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.Status.Should().Be("Success");

            _visitRepoMock.Verify(r => r.AddAsync(It.IsAny<PlaceVisit>()), Times.Once);

            _notificationMock.Verify(n => n.NotifyPlaceAdded(trip.Id, It.IsAny<object>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldReturnUnauthorized_WhenUserIsNotMember()
        {
            var creatorId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid(); 
            var trip = new TripBuilder().WithCreator(creatorId).Build();

            _tripRepoMock.Setup(r => r.AsQueryable())
                .Returns(new List<Trip> { trip }.AsQueryable());

            var cmd = new AddPlaceToTripCommand(trip.Id, otherUserId);

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.Status.Should().Be("Error");

            result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            _visitRepoMock.Verify(r => r.AddAsync(It.IsAny<PlaceVisit>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldReturnError_WhenPlaceAlreadyInTrip()
        {
            var userId = Guid.NewGuid();
            var place = new PlaceBuilder().Build();
            var trip = new TripBuilder().WithCreator(userId).Build();

            trip.PlaceVisits.Add(new PlaceVisit { PlaceId = place.Id, TripId = trip.Id });

            _tripRepoMock.Setup(r => r.AsQueryable())
                .Returns(new List<Trip> { trip }.AsQueryable());

            var cmd = new AddPlaceToTripCommand(trip.Id, place.Id);

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.Status.Should().Be("Error");

            result.Message.Should().Contain("zaten listede");

            _visitRepoMock.Verify(r => r.AddAsync(It.IsAny<PlaceVisit>()), Times.Never);
        }
    }
}
