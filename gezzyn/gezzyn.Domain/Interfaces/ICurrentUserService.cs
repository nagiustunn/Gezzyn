namespace gezzyn.Domain.Interfaces
{
    public interface ICurrentUserService
    {
        string? UserId { get; }
        string GetUnAuthUserSessionId();
    }
}
