using FluentAssertions;
using gezzyn.Application.Features.Trips.Queries.GetTripById;
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
    public class GetTripByIdQueryHandlerTests
    {
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();
        private readonly Mock<IRepository<Trip>> _tripRepoMock = new();
        private readonly Guid _currentUserId = Guid.NewGuid();
        private readonly GetTripByIdQueryHandler _handler;

        public GetTripByIdQueryHandlerTests()
        {
            _uowMock.Setup(u => u.Repository<Trip>()).Returns(_tripRepoMock.Object);
            SetupHttpContextWithUser(_currentUserId);

            _handler = new GetTripByIdQueryHandler(_uowMock.Object, _httpContextAccessorMock.Object);
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
        public async Task Handle_ShouldReturnTrip_WhenUserIsMember()
        {
            var trip = new TripBuilder()
                           .WithCreator(_currentUserId)
                           .WithTitle("Kız Kıza Mardin Turu")
                           .WithCity("Mardin")
                           .Build();

            _tripRepoMock.Setup(r => r.AsQueryable())
                         .Returns(new List<Trip> { trip }.AsQueryable());

            var query = new GetTripByIdQuery (trip.Id);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.Status.Should().Be("Success");
            result.Data.Should().NotBeNull();
            result.Data!.Title.Should().Be("Kız Kıza Mardin Turu");
            result.Data.City.Should().Be("Mardin");
            result.Data.Members.Should().HaveCount(1);
        }

        [Fact]
        public async Task Handle_ShouldReturnNotFound_WhenTripDoesNotExist()
        {
            _tripRepoMock.Setup(r => r.AsQueryable())
                .Returns(new List<Trip>().AsQueryable());

            var query = new GetTripByIdQuery(Guid.NewGuid());

            var result = await _handler.Handle(query, CancellationToken.None);

            result.Status.Should().Be("Error");
            result.StatusCode.Should().Be(HttpStatusCode.NotFound);
            result.Data.Should().BeNull();
        }

        [Fact]
        public async Task Handle_ShouldReturnUnauthorized_WhenUserIsNotMember()
        {
            var anotherUserId = Guid.NewGuid();
            SetupHttpContextWithUser(anotherUserId);

            var creatorId = Guid.NewGuid();
            var trip = new TripBuilder().WithCreator(creatorId).Build();

            _tripRepoMock.Setup(r => r.AsQueryable())
                .Returns(new List<Trip> { trip }.AsQueryable());

            var query = new GetTripByIdQuery (trip.Id);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.Status.Should().Be("Error");
            result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Handle_ShouldReturnTrip_WithCorrectPlaceCount()
        {
            var trip = new TripBuilder().WithCreator(_currentUserId).Build();

            trip.PlaceVisits.Add(new PlaceVisit { PlaceId = Guid.NewGuid(), Order = 1, IsDeleted = false, Place = new PlaceBuilder().Build() });
            trip.PlaceVisits.Add(new PlaceVisit { PlaceId = Guid.NewGuid(), Order = 2, IsDeleted = false, Place = new PlaceBuilder().Build() });
            trip.PlaceVisits.Add(new PlaceVisit { PlaceId = Guid.NewGuid(), Order = 3, IsDeleted = false, Place = new PlaceBuilder().Build() });

            _tripRepoMock.Setup(r => r.AsQueryable())
                         .Returns(new List<Trip> { trip }.AsQueryable());

            var query = new GetTripByIdQuery (trip.Id);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.Data!.PlaceCount.Should().Be(3);
            result.Data.PlaceVisits.Should().HaveCount(3);
            result.Data.PlaceVisits.Should().BeInAscendingOrder(pv => pv.Order);
        }

        [Fact]
        public async Task Handle_ShouldReturnTrip_WithCorrectMemberRoles()
        {
            var trip = new TripBuilder().WithCreator(_currentUserId).Build();

            var editorId = Guid.NewGuid();
            trip.Members.Add(new TripMember
            {
                UserId = editorId,
                TripId = trip.Id,
                Role = TripMemberRole.Editor,
                User = new UserBuilder().WithId(editorId).Build()
            });

            _tripRepoMock.Setup(r => r.AsQueryable())
                         .Returns(new List<Trip> { trip }.AsQueryable());

            var query = new GetTripByIdQuery (trip.Id);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.Data!.Members.Should().HaveCount(2);
            result.Data.Members.Should().Contain(m => m.Role == "Admin");
            result.Data.Members.Should().Contain(m => m.Role == "Editor");
        }
    }
}
