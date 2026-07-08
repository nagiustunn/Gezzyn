using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using gezzyn.Application.Features.Trips.Commands.ReOrderPlaces;
using gezzyn.Domain.Entities;
using gezzyn.Domain.Enums;
using gezzyn.Domain.Interfaces;
using gezzyn.Tests.Unit.Common.Builders;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Net;
using System.Security.Claims;

namespace gezzyn.Tests.Unit.Features.Trips
{
    public class ReorderPlacesCommandHandlerTests
    {
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();
        private readonly Mock<ITripNotificationService> _tripNotificationServiceMock = new();
        private readonly Mock<IValidator<ReOrderPlacesCommand>> _validatorMock = new();
        private readonly Mock<IRepository<Trip>> _tripRepoMock = new();
        private readonly Mock<IRepository<PlaceVisit>> _visitRepoMock = new();
        private readonly Guid _currentUserId = Guid.NewGuid();
        private readonly ReOrderPlacesCommandHandler _handler;

        public ReorderPlacesCommandHandlerTests()
        {
            _uowMock.Setup(u => u.Repository<Trip>()).Returns(_tripRepoMock.Object);
            _uowMock.Setup(u => u.Repository<PlaceVisit>()).Returns(_visitRepoMock.Object);
            _uowMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            _validatorMock.Setup(v => v.ValidateAsync(
                          It.IsAny<ReOrderPlacesCommand>(),
                          It.IsAny<CancellationToken>()))
                          .ReturnsAsync(new ValidationResult());

            SetupHttpContextWithUser(_currentUserId);

            _handler = new ReOrderPlacesCommandHandler(_uowMock.Object, _httpContextAccessorMock.Object, _tripNotificationServiceMock.Object, _validatorMock.Object);
        }

        private void SetupHttpContextWithUser(Guid userId)
        {
            var claims = new List<Claim>
                         {
                             new Claim(ClaimTypes.NameIdentifier, userId.ToString())
                         };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);
        }

        [Fact]
        public async Task Handle_ShouldReorderPlaces_WhenUserIsAdminOrEditor()
        {
            var placeId1 = Guid.NewGuid();
            var placeId2 = Guid.NewGuid();
            var placeId3 = Guid.NewGuid();

            var trip = new TripBuilder().WithCreator(_currentUserId).Build();
            trip.PlaceVisits.Add(new PlaceVisit { PlaceId = placeId1, Order = 1, IsDeleted = false });
            trip.PlaceVisits.Add(new PlaceVisit { PlaceId = placeId2, Order = 2, IsDeleted = false });
            trip.PlaceVisits.Add(new PlaceVisit { PlaceId = placeId3, Order = 3, IsDeleted = false });

            _tripRepoMock.Setup(r => r.AsQueryable())
                .Returns(new List<Trip> { trip }.AsQueryable());

            var newOrder = new List<Guid> { placeId3, placeId1, placeId2 };
            var cmd = new ReOrderPlacesCommand (trip.Id, newOrder);

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.Status.Should().Be("Success");
            _uowMock.Verify(u => u.SaveChangesAsync(), Times.Once);

            trip.PlaceVisits.First(pv => pv.PlaceId == placeId3).Order.Should().Be(1);
            trip.PlaceVisits.First(pv => pv.PlaceId == placeId1).Order.Should().Be(2);
            trip.PlaceVisits.First(pv => pv.PlaceId == placeId2).Order.Should().Be(3);
        }

        [Fact]
        public async Task Handle_ShouldReturnUnauthorized_WhenUserIsRegularMember()
        {
            var trip = new TripBuilder().WithCreator(Guid.NewGuid()).Build();
            trip.Members.Add(new TripMember
            {
                UserId = _currentUserId,
                TripId = trip.Id,
                Role = TripMemberRole.Member 
            });

            _tripRepoMock.Setup(r => r.AsQueryable())
                         .Returns(new List<Trip> { trip }.AsQueryable());

            var cmd = new ReOrderPlacesCommand(trip.Id, new List<Guid> { Guid.NewGuid() });

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.Status.Should().Be("Error");
            result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            _uowMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldReturnNotFound_WhenTripDoesNotExist()
        {
            _tripRepoMock.Setup(r => r.AsQueryable())
                         .Returns(new List<Trip>().AsQueryable());

            var cmd = new ReOrderPlacesCommand(Guid.NewGuid(), new List<Guid> { Guid.NewGuid() });

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.Status.Should().Be("Error");
            result.StatusCode.Should().Be(HttpStatusCode.NotFound);
            _uowMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldReturnUnauthorized_WhenUserIsNotMember()
        {
            var trip = new TripBuilder().WithCreator(Guid.NewGuid()).Build();

            _tripRepoMock.Setup(r => r.AsQueryable())
                .Returns(new List<Trip> { trip }.AsQueryable());

            var cmd = new ReOrderPlacesCommand(trip.Id, new List<Guid> { Guid.NewGuid() });

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.Status.Should().Be("Error");
            result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Handle_ShouldAllowEditor_ToReorderPlaces()
        {
            var placeId1 = Guid.NewGuid();
            var placeId2 = Guid.NewGuid();

            var trip = new TripBuilder().WithCreator(Guid.NewGuid()).Build();
            trip.Members.Add(new TripMember
            {
                UserId = _currentUserId,
                TripId = trip.Id,
                Role = TripMemberRole.Editor 
            });
            trip.PlaceVisits.Add(new PlaceVisit { PlaceId = placeId1, Order = 1, IsDeleted = false });
            trip.PlaceVisits.Add(new PlaceVisit { PlaceId = placeId2, Order = 2, IsDeleted = false });

            _tripRepoMock.Setup(r => r.AsQueryable())
                .Returns(new List<Trip> { trip }.AsQueryable());

            var cmd = new ReOrderPlacesCommand(trip.Id, new List<Guid> { placeId2, placeId1 });

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.Status.Should().Be("Success");
            trip.PlaceVisits.First(pv => pv.PlaceId == placeId2).Order.Should().Be(1);
            trip.PlaceVisits.First(pv => pv.PlaceId == placeId1).Order.Should().Be(2);
        }
    }
}
