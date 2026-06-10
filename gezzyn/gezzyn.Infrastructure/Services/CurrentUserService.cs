using gezzyn.Domain.Interfaces;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http; 

namespace gezzyn.Infrastructure.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string? UserId => _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        public string GetUnAuthUserSessionId()
        {
            var userId = "";
            if (string.IsNullOrEmpty(UserId))
            {
                if (_httpContextAccessor.HttpContext.Session.TryGetValue("GuestUserId", out var value) && value != null)
                {
                    userId = Encoding.UTF8.GetString(value);
                }

                if (string.IsNullOrEmpty(userId))
                {
                    userId = Guid.NewGuid().ToString();
                    _httpContextAccessor.HttpContext.Session.Set("GuestUserId", Encoding.UTF8.GetBytes(userId));
                }
            }

            return userId;
        }
    }
}
