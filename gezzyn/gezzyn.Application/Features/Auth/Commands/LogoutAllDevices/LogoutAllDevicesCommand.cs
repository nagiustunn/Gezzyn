using gezzyn.Domain.DTO;
using MediatR;

namespace gezzyn.Application.Features.Auth.Commands.LogoutAllDevices
{
    public record LogoutAllDevicesCommand() : IRequest<Response<bool>>; 
}
