using gezzyn.Application.DTO.Auth;
using gezzyn.Domain.DTO;
using MediatR;

namespace gezzyn.Application.Features.Auth.Commands.RefreshToken
{
    public record RefreshTokenCommand(string RefreshToken) : IRequest<Response<AuthResponseDto>>;
}
