using gezzyn.Application.DTO.Trip;
using gezzyn.Domain.DTO;
using MediatR;

namespace gezzyn.Application.Features.Trips.Queries.GetTripById
{
    public record GetTripByIdQuery(Guid TripId) : IRequest<Response<TripDto>>;
}
