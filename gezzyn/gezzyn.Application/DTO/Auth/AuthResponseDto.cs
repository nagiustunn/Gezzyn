using gezzyn.Application.DTO.User;

namespace gezzyn.Application.DTO.Auth
{
    public class AuthResponseDto
    {
        public string? AccessToken { get; set; } 
        public string? RefreshToken { get; set; }
        public DateTime AccessTokenExpiresAt { get; set; }
        public UserDto? User { get; set; } 
    }
}
