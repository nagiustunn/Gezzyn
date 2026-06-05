using gezzyn.Domain.Enums;

namespace gezzyn.Domain.Entities
{
    public class PlaceVisit : BaseEntity
    {
        public Guid TripId { get; set; }
        public Guid PlaceId { get; set; }
        public Guid AddedByUserId { get; set; }
        public int Order { get; set; }
        public VisitStatus Status { get; set; } = VisitStatus.Planned;
        public string? PlannedArrivalTime { get; set; }
        public int? EstimatedDurationMinutes { get; set; }
        public string? Note { get; set; }

        public Trip? Trip { get; set; }
        public Place? Place { get; set; }
        public User? AddedBy { get; set; }
    }
}
