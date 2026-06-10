using gezzyn.API.Extensions;
using gezzyn.Application.Features.Auth.Commands.Login;
using gezzyn.Application.Features.Auth.Commands.Logout;
using gezzyn.Application.Features.Auth.Commands.LogoutAllDevices;
using gezzyn.Application.Features.Auth.Commands.RefreshToken;
using gezzyn.Application.Features.Auth.Commands.Register;
using gezzyn.Application.Features.Auth.Queries.GetAuthUser;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace gezzyn.API.Controllers
{
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AuthController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetAuthUser()
        {
            var result = await _mediator.Send(new GetAuthUserQuery());
            return result.ToActionResult();
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromBody] LogoutCommand command)
        {
            var result = await _mediator.Send(command);
            return result.ToActionResult();
        }

        [HttpPost("logout-all-devices")]
        [Authorize]
        public async Task<IActionResult> LogoutAllDevices()
        {
            var result = await _mediator.Send(new LogoutAllDevicesCommand());
            return result.ToActionResult();
        }
    }
}
