using FluentAssertions;
using FluentValidation;
using gezzyn.Application.Features.Trips.Commands.OptimizeRoute;
using gezzyn.Domain.DTO.Route;
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
    public class OptimizeRouteCommandHandlerTests
    {
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<IRouteOptimizationService> _routeServiceMock = new();
        private readonly Mock<ITripNotificationService> _notificationMock = new();
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();
        private readonly Mock<IValidator<OptimizeRouteCommand>> _validatorMock = new();
        private readonly Mock<IRepository<Trip>> _tripRepoMock = new();
        private readonly Mock<IRepository<PlaceVisit>> _visitRepoMock = new();
        private readonly Guid _currentUserId = Guid.NewGuid();
        private readonly OptimizeRouteCommandHandler _handler;

        public OptimizeRouteCommandHandlerTests()
        {
            _uowMock.Setup(u => u.Repository<Trip>()).Returns(_tripRepoMock.Object);
            _uowMock.Setup(u => u.Repository<PlaceVisit>()).Returns(_visitRepoMock.Object);
            _uowMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

            _notificationMock.Setup(n => n.NotifyRouteOptimized(It.IsAny<Guid>(), It.IsAny<object>()))
                             .Returns(Task.CompletedTask);

            SetupHttpContextWithUser(_currentUserId);

            _handler = new OptimizeRouteCommandHandler(_uowMock.Object, _httpContextAccessorMock.Object, _routeServiceMock.Object, _notificationMock.Object, _validatorMock.Object);
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

        private Trip BuildTripWithPlaces(Guid creatorId, int placeCount)
        {
            var trip = new TripBuilder().WithCreator(creatorId).Build();

            for (int i = 0; i < placeCount; i++)
            {
                var place = new PlaceBuilder()
                    .WithCoordinates(37.29 + i * 0.01, 40.78 + i * 0.01)
                    .Build();

                trip.PlaceVisits.Add(new PlaceVisit
                {
                    PlaceId = place.Id,
                    Order = i + 1,
                    IsDeleted = false,
                    Place = place
                });
            }

            return trip;
        }

        [Fact]
        public async Task Handle_ShouldOptimizeRoute_AndUpdatePlaceVisitOrders()
        {
            var trip = BuildTripWithPlaces(_currentUserId, 3);
            var visits = trip.PlaceVisits.ToList();

            var placeId1 = visits[0].PlaceId;
            var placeId2 = visits[1].PlaceId;
            var placeId3 = visits[2].PlaceId;

            _tripRepoMock.Setup(r => r.AsQueryable())
                         .Returns(new List<Trip> { trip }.AsQueryable());

            _routeServiceMock.Setup(r => r.OptimizeRouteAsync(It.IsAny<List<RoutePoint>>(), It.IsAny<TravelMode>(), It.IsAny<CancellationToken>()))
                             .ReturnsAsync(new RouteOptimizationResult
                             {
                                 OptimizedPlaceIds = new List<Guid> { placeId3, placeId1, placeId2 },
                                 TotalDistanceMeters = 4200,
                                 TotalDurationSeconds = 1800,
                                 EncodedPolyline = "encoded_polyline_data"
                             });

            var cmd = new OptimizeRouteCommand(trip.Id, "Drive");

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.Status.Should().Be("Success");
            result.Data.Should().NotBeNull();
            result.Data!.TotalDistanceMeters.Should().Be(4200);
            result.Data.TotalDurationSeconds.Should().Be(1800);
            result.Data.EncodedPolyline.Should().Be("encoded_polyline_data");

            trip.PlaceVisits.First(pv => pv.PlaceId == placeId3).Order.Should().Be(1);
            trip.PlaceVisits.First(pv => pv.PlaceId == placeId1).Order.Should().Be(2);
            trip.PlaceVisits.First(pv => pv.PlaceId == placeId2).Order.Should().Be(3);

            _uowMock.Verify(u => u.SaveChangesAsync(), Times.Once);

            _notificationMock.Verify(n => n.NotifyRouteOptimized(trip.Id, It.IsAny<object>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldReturnBadRequest_WhenLessThanTwoPlaces()
        {
            var trip = BuildTripWithPlaces(_currentUserId, 1);

            _tripRepoMock.Setup(r => r.AsQueryable())
                          .Returns(new List<Trip> { trip }.AsQueryable());

            var cmd = new OptimizeRouteCommand(trip.Id, "Drive");

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.Status.Should().Be("Error");
            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            result.Message.Should().Contain("en az 2 mekan");

            _routeServiceMock.Verify(r => r.OptimizeRouteAsync(It.IsAny<List<RoutePoint>>(), It.IsAny<TravelMode>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldReturnUnauthorized_WhenUserIsNotMember()
        {
            var anotherUserId = Guid.NewGuid();
            SetupHttpContextWithUser(anotherUserId);

            var trip = BuildTripWithPlaces(Guid.NewGuid(), 3);

            _tripRepoMock.Setup(r => r.AsQueryable())
                .Returns(new List<Trip> { trip }.AsQueryable());

            var cmd = new OptimizeRouteCommand(trip.Id, "Drive");

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.Status.Should().Be("Error");
            result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            _routeServiceMock.Verify(r => r.OptimizeRouteAsync(It.IsAny<List<RoutePoint>>(), It.IsAny<TravelMode>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldReturnNotFound_WhenTripDoesNotExist()
        {
            _tripRepoMock.Setup(r => r.AsQueryable())
                         .Returns(new List<Trip>().AsQueryable());

            var cmd = new OptimizeRouteCommand(Guid.NewGuid(), "Drive");

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.Status.Should().Be("Error");
            result.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Handle_ShouldReturnBadRequest_WhenGoogleRoutesApiReturnsNull()
        {
            var trip = BuildTripWithPlaces(_currentUserId, 3);

            _tripRepoMock.Setup(r => r.AsQueryable())
                         .Returns(new List<Trip> { trip }.AsQueryable());

            _routeServiceMock.Setup(r => r.OptimizeRouteAsync(It.IsAny<List<RoutePoint>>(), It.IsAny<TravelMode>(), It.IsAny<CancellationToken>()))
                             .ReturnsAsync((RouteOptimizationResult?)null);

            var cmd = new OptimizeRouteCommand(trip.Id, "Drive");

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.Status.Should().Be("Error");
            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            result.Message.Should().Contain("Rota hesaplanamadı");

            _uowMock.Verify(u => u.SaveChangesAsync(), Times.Never);

            _notificationMock.Verify(n => n.NotifyRouteOptimized(It.IsAny<Guid>(), It.IsAny<object>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldSkipPlacesWithoutCoordinates()
        {
            var trip = new TripBuilder().WithCreator(_currentUserId).Build();

            var placeWithCoords1 = new PlaceBuilder().WithCoordinates(37.294, 40.783).Build();
            var placeWithCoords2 = new PlaceBuilder().WithCoordinates(37.312, 40.731).Build();
            var placeWithoutCoords = new PlaceBuilder().Build();
            placeWithoutCoords.Latitude = null;
            placeWithoutCoords.Longitude = null;

            trip.PlaceVisits.Add(new PlaceVisit
            { PlaceId = placeWithCoords1.Id, Order = 1, IsDeleted = false, Place = placeWithCoords1 });
            trip.PlaceVisits.Add(new PlaceVisit
            { PlaceId = placeWithCoords2.Id, Order = 2, IsDeleted = false, Place = placeWithCoords2 });
            trip.PlaceVisits.Add(new PlaceVisit
            { PlaceId = placeWithoutCoords.Id, Order = 3, IsDeleted = false, Place = placeWithoutCoords });

            _tripRepoMock.Setup(r => r.AsQueryable())
                         .Returns(new List<Trip> { trip }.AsQueryable());

            _routeServiceMock.Setup(r => r.OptimizeRouteAsync(It.Is<List<RoutePoint>>(pts => pts.Count == 2), It.IsAny<TravelMode>(), It.IsAny<CancellationToken>()))
                             .ReturnsAsync(new RouteOptimizationResult
                             {
                                 OptimizedPlaceIds = new List<Guid> { placeWithCoords1.Id, placeWithCoords2.Id },
                                 TotalDistanceMeters = 2100,
                                 TotalDurationSeconds = 900
                             });

            var cmd = new OptimizeRouteCommand(trip.Id, "Walk");

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.Status.Should().Be("Success");

            _routeServiceMock.Verify(r => r.OptimizeRouteAsync(It.Is<List<RoutePoint>>(pts => pts.Count == 2), TravelMode.Walk, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
