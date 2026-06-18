using gezzyn.Application.DTO.Trip;
using gezzyn.Domain.DTO;
using MediatR;

namespace gezzyn.Application.Features.Trips.Queries.GetMyTrips
{
    public record GetMyTripsQuery() : IRequest<Response<List<TripDto>>>;
}
