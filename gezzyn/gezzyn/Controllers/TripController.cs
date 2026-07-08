using gezzyn.API.Extensions;
using gezzyn.Application.DTO.Trip;
using gezzyn.Application.Features.Trips.Commands.AddPlaceToTrip;
using gezzyn.Application.Features.Trips.Commands.CreateTrip;
using gezzyn.Application.Features.Trips.Commands.OptimizeRoute;
using gezzyn.Application.Features.Trips.Commands.ReOrderPlaces;
using gezzyn.Application.Features.Trips.Queries.GetMyTrips;
using gezzyn.Application.Features.Trips.Queries.GetTripById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace gezzyn.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TripController : ControllerBase
    {
        private readonly IMediator _mediator;

        public TripController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAllTrip()
        {
            var result = await _mediator.Send(new GetMyTripsQuery());
            return result.ToActionResult();
        }

        [HttpGet("{id:guid}")]
        [Authorize]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _mediator.Send(new GetTripByIdQuery(id));
            return result.ToActionResult();
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTripDto createTripDto)
        {
            var result = await _mediator.Send(new CreateTripCommand(createTripDto));
            return result.ToActionResult();
        }

        [HttpPost("{id:guid}/places")]
        public async Task<IActionResult> AddPlace(Guid id, [FromBody] Guid placeId, CancellationToken ct)
        {
            var result = await _mediator.Send(new AddPlaceToTripCommand(id, placeId));
            return result.ToActionResult();
        }

        [HttpPut("{id:guid}/reorder")]
        public async Task<IActionResult> Reorder(Guid id, [FromBody] List<Guid> orderPlacesIds)
        {
            var result = await _mediator.Send(new ReOrderPlacesCommand(id, orderPlacesIds));
            return result.ToActionResult();
        }

        [HttpPost("{id:guid}/optimize")]
        public async Task<IActionResult> OptimizeRoute(  Guid id, [FromQuery] string travelMode = "Drive")
        {
            var result = await _mediator.Send(new OptimizeRouteCommand(id, travelMode));
            return result.ToActionResult();
        }
    }
}
