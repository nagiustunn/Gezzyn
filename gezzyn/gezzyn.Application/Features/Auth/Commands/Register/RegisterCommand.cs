using gezzyn.Application.DTO.Auth;
using gezzyn.Domain.DTO;
using MediatR;

namespace gezzyn.Application.Features.Auth.Commands.Register
{
    public record RegisterCommand( string Name, string Surname, string UserName, string Email,  string Password) : IRequest<Response<AuthResponseDto>>;
}
