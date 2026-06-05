using gezzyn.Domain.Enums;

namespace gezzyn.Domain.Entities
{
    public class TripMember : BaseEntity
    {
        public Guid TripId { get; set; }
        public Guid UserId { get; set; }
        public TripMemberRole Role { get; set; } = TripMemberRole.Member;
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        public Trip? Trip { get; set; } 
        public User? User { get; set; }
    }
}
