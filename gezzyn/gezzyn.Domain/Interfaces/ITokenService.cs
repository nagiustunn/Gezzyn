using gezzyn.Domain.Entities;

namespace gezzyn.Domain.Interfaces
{
    public interface ITokenService
    {
        string GenerateToken(User user);
        string GenerateRefreshToken();
        Guid? GetUserIdFromExpiredToken(string token);
    }
}
