namespace gezzyn.Domain.Entities
{
    public class User : BaseEntity
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? Surname { get; set; }
        public string? UserName { get; set; }
        public string? PasswordHash { get; set; } 
        public string? AvatarUrl { get; set; }
        public bool IsEmailVerified { get; set; } = false;

        public ICollection<TripMember>? TripMemberships { get; set; }
        public ICollection<RefreshToken>? RefreshTokens { get; set; } 

        public string FullName => $"{Name} {Surname}";
    }
}
