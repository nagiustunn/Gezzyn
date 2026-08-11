using FluentAssertions;
using gezzyn.Application.Features.Trips.Queries.GetMyTrips;
using gezzyn.Domain.Entities;
using gezzyn.Domain.Enums;
using gezzyn.Domain.Interfaces;
using gezzyn.Tests.Unit.Common.Builders;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Security.Claims;

namespace gezzyn.Tests.Unit.Features.Trips
{
    public class GetMyTripsQueryHandlerTests
    {
        private readonly Mock<IUnitOfWork> _uowMock = new();
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock = new();
        private readonly Mock<IRepository<Trip>> _tripRepoMock = new();
        private readonly Guid _currentUserId = Guid.NewGuid();
        private readonly GetMyTripsQueryHandler _handler;

        public GetMyTripsQueryHandlerTests()
        {
            _uowMock.Setup(u => u.Repository<Trip>()).Returns(_tripRepoMock.Object);
            SetupHttpContextWithUser(_currentUserId);

            _handler = new GetMyTripsQueryHandler(_uowMock.Object, _httpContextAccessorMock.Object);
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
        public async Task Handle_ShouldReturnOnlyUserTrips()
        {
            var otherUserId = Guid.NewGuid();

            var myTrip1 = new TripBuilder().WithCreator(_currentUserId).WithTitle("Mardin Turu").Build();
            var myTrip2 = new TripBuilder().WithCreator(_currentUserId).WithTitle("Kapadokya Turu").Build();
            var otherTrip = new TripBuilder().WithCreator(otherUserId).WithTitle("Efes Turu").Build();

            _tripRepoMock.Setup(r => r.AsQueryable())
                         .Returns(new List<Trip> { myTrip1, myTrip2, otherTrip }.AsQueryable());

            var query = new GetMyTripsQuery();

            var result = await _handler.Handle(query, CancellationToken.None);

            result.Status.Should().Be("Success");
            result.Data.Should().HaveCount(2);
            result.Data.Should().Contain(t => t.Title == "Mardin Turu");
            result.Data.Should().Contain(t => t.Title == "Kapadokya Turu");
            result.Data.Should().NotContain(t => t.Title == "Efes Turu");
        }

        [Fact]
        public async Task Handle_ShouldReturnEmptyList_WhenUserHasNoTrips()
        {
            var otherUserId = Guid.NewGuid();
            var otherTrip = new TripBuilder().WithCreator(otherUserId).Build();

            _tripRepoMock.Setup(r => r.AsQueryable())
                         .Returns(new List<Trip> { otherTrip }.AsQueryable());

            var query = new GetMyTripsQuery();

            var result = await _handler.Handle(query, CancellationToken.None);

            result.Status.Should().Be("Success");
            result.Data.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_ShouldNotReturnSoftDeletedTrips()
        {
            var activeTrip = new TripBuilder()
                                 .WithCreator(_currentUserId)
                                 .WithTitle("Aktif Gezi")
                                 .Build();

            var deletedTrip = new TripBuilder()
                                  .WithCreator(_currentUserId)
                                  .WithTitle("Silinmiş Gezi")
                                  .Build();

            deletedTrip.IsDeleted = true;

            _tripRepoMock.Setup(r => r.AsQueryable())
                         .Returns(new List<Trip> { activeTrip, deletedTrip }.AsQueryable());

            var query = new GetMyTripsQuery();

            var result = await _handler.Handle(query, CancellationToken.None);

            result.Data.Should().HaveCount(1);
            result.Data.Should().Contain(t => t.Title == "Aktif Gezi");
            result.Data.Should().NotContain(t => t.Title == "Silinmiş Gezi");
        }

        [Fact]
        public async Task Handle_ShouldReturnTrips_WhenUserIsGuestMember()
        {
            var creatorId = Guid.NewGuid();
            var guestTrip = new TripBuilder().WithCreator(creatorId).WithTitle("Davet Edilen Gezi").Build();

            guestTrip.Members.Add(new TripMember
            {
                UserId = _currentUserId,
                TripId = guestTrip.Id,
                Role = TripMemberRole.Member,
                IsDeleted = false
            });

            _tripRepoMock.Setup(r => r.AsQueryable())
                         .Returns(new List<Trip> { guestTrip }.AsQueryable());

            var query = new GetMyTripsQuery();

            var result = await _handler.Handle(query, CancellationToken.None);

            result.Data.Should().HaveCount(1);
            result.Data[0].Title.Should().Be("Davet Edilen Gezi");
        }

        [Fact]
        public async Task Handle_ShouldReturnTrips_OrderedByCreatedAtDescending()
        {
            var oldTrip = new TripBuilder().WithCreator(_currentUserId).WithTitle("Eski Gezi").Build();
            oldTrip.CreatedAt = DateTime.UtcNow.AddDays(-10);

            var newTrip = new TripBuilder().WithCreator(_currentUserId).WithTitle("Yeni Gezi").Build();
            newTrip.CreatedAt = DateTime.UtcNow;

            _tripRepoMock.Setup(r => r.AsQueryable())
                         .Returns(new List<Trip> { oldTrip, newTrip }.AsQueryable());

            var query = new GetMyTripsQuery();

            var result = await _handler.Handle(query, CancellationToken.None);

            result.Data[0].Title.Should().Be("Yeni Gezi");
            result.Data[1].Title.Should().Be("Eski Gezi");
        }

        [Fact]
        public async Task Handle_ShouldNotReturnTrips_WhenMembershipIsSoftDeleted()
        {
            var trip = new TripBuilder().WithCreator(Guid.NewGuid()).WithTitle("Çıkarıldığım Gezi").Build();

            trip.Members.Add(new TripMember
            {
                UserId = _currentUserId,
                TripId = trip.Id,
                Role = TripMemberRole.Member,
                IsDeleted = true
            });

            _tripRepoMock.Setup(r => r.AsQueryable())
                .Returns(new List<Trip> { trip }.AsQueryable());

            var query = new GetMyTripsQuery();

            var result = await _handler.Handle(query, CancellationToken.None);

            result.Data.Should().BeEmpty();
        }
    }
}
