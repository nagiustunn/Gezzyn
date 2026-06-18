using gezzyn.Domain.DTO;
using MediatR;

namespace gezzyn.Application.Features.Trips.Commands.AddPlaceToTrip
{
    public record AddPlaceToTripCommand(Guid TripId, Guid PlaceId) : IRequest<Response<bool>>;
}
