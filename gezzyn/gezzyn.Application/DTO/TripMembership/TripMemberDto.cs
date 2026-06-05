namespace gezzyn.Application.DTO.TripMembership
{
    public class TripMemberDto
    {
        public Guid UserId { get; set; }
        public string? FullName { get; set; }
        public string? UserName { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Role { get; set; }
        public DateTime JoinedAt { get; set; }
    }
}
