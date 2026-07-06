using gezzyn.API.Extensions;
using gezzyn.Application.Features.Places.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace gezzyn.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PlacesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PlacesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("import")]
        public async Task<IActionResult> ImportFromGoogle([FromQuery] string city, [FromQuery] string searchQuery, CancellationToken ct)
        {
            var result = await _mediator.Send(new ImportPlacesFromGoogleCommand(city, searchQuery));
            return result.ToActionResult();
        }
    }
}
