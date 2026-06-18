using gezzyn.Application.DTO.Trip;
using gezzyn.Domain.DTO;
using MediatR;

namespace gezzyn.Application.Features.Trips.Commands.CreateTrip
{
    public record CreateTripCommand(CreateTripDto CreateTripDto) : IRequest<Response<bool>>;
}
