using gezzyn.Domain.DTO;
using MediatR;

namespace gezzyn.Application.Features.Trips.Commands.ReOrderPlaces
{
    public record ReOrderPlacesCommand(Guid TripId, List<Guid> OrderPlacesIds) : IRequest<Response<bool>>;
}
