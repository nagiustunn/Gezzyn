using gezzyn.Application.DTO.Place;
using gezzyn.Domain.DTO;
using MediatR;

namespace gezzyn.Application.Features.Places.Commands
{
    public record ImportPlacesFromGoogleCommand(string City, string SearchQuery) : IRequest<Response<List<PlaceDto>>>;
}
