using gezzyn.Domain.Enums;

namespace gezzyn.Domain.Entities
{
    public class Trip : BaseEntity
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? City { get; set; } 
        public string? CoverImageUrl { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public TripStatus Status { get; set; } = TripStatus.Planning;
        public string InviteCode { get; set; } = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        public Guid CreatedByUserId { get; set; }

        public User? CreatedBy { get; set; } 
        public ICollection<TripMember>? Members { get; set; } 
        public ICollection<PlaceVisit>? PlaceVisits { get; set; }
    }
}
