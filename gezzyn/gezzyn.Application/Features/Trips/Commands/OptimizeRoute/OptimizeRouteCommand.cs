using gezzyn.Application.DTO.Route;
using gezzyn.Domain.DTO;
using MediatR;

namespace gezzyn.Application.Features.Trips.Commands.OptimizeRoute
{
    public record OptimizeRouteCommand(Guid TripId, string TravelMode) : IRequest<Response<OptimizeRouteResultDto>>;
}
