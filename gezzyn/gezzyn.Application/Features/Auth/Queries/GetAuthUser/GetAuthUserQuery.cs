using gezzyn.Application.DTO.User;
using gezzyn.Domain.DTO;
using MediatR;

namespace gezzyn.Application.Features.Auth.Queries.GetAuthUser
{
    public record GetAuthUserQuery() : IRequest<Response<UserDto>>;
}
