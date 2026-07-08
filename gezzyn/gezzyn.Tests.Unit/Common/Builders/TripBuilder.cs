using gezzyn.Domain.Entities;
using gezzyn.Domain.Enums;

namespace gezzyn.Tests.Unit.Common.Builders
{
    public class TripBuilder
    {
        private Guid _id = Guid.NewGuid();
        private string _title = "Test Gezisi";
        private string _city = "Mardin";
        private Guid _creatorId = Guid.NewGuid();

        public TripBuilder WithId(Guid id) { _id = id; return this; }
        public TripBuilder WithTitle(string title) { _title = title; return this; }
        public TripBuilder WithCity(string city) { _city = city; return this; }
        public TripBuilder WithCreator(Guid creatorId) { _creatorId = creatorId; return this; }

        public Trip Build()
        {
            var trip = new Trip
            {
                Id = _id,
                Title = _title,
                City = _city,
                CreatedByUserId = _creatorId
            };

            trip.Members.Add(new TripMember
            {
                TripId = _id,
                UserId = _creatorId,
                Role = TripMemberRole.Admin
            });

            return trip;
        }
    }
}
