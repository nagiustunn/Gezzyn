using gezzyn.Domain.DTO;
using MediatR;

namespace gezzyn.Application.Features.Auth.Commands.Logout
{
    public record LogoutCommand(string RefreshToken) : IRequest<Response<bool>>;
}
