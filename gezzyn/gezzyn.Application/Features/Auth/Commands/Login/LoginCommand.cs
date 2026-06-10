using gezzyn.Application.DTO.Auth;
using gezzyn.Domain.DTO;
using MediatR;

namespace gezzyn.Application.Features.Auth.Commands.Login
{
    public record LoginCommand(string Email, string Password) : IRequest<Response<AuthResponseDto>>;
}
