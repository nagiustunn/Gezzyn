using gezzyn.Application.DTO.PlaceVisit;
using gezzyn.Application.DTO.TripMembership;

namespace gezzyn.Application.DTO.Trip
{
    public class TripDto
    {
        public Guid Id { get; set; }
        public string? Title { get; set; }
        public string? City { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; }
        public string? InviteCode { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public int MemberCount { get; set; }
        public int PlaceCount { get; set; }
        public List<TripMemberDto>? Members { get; set; }
        public List<PlaceVisitDto>? PlaceVisits { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
