using System.Net;
using gezzyn.Domain.DTO;
using Microsoft.AspNetCore.Mvc;

namespace gezzyn.API.Extensions;

public static class ControllerExtensions
{
    public static IActionResult ToActionResult<T>(this Response<T> response)
    {
        return response.StatusCode switch
        {
            HttpStatusCode.OK => new OkObjectResult(response),
            HttpStatusCode.NoContent => new NoContentResult(),
            HttpStatusCode.NotFound => new NotFoundObjectResult(response),
            HttpStatusCode.BadRequest => new BadRequestObjectResult(response),
            HttpStatusCode.Unauthorized => new UnauthorizedObjectResult(response),
            _ => new ObjectResult(response)
            {
                StatusCode = (int)response.StatusCode
            }
        };
    }

    public static IActionResult ToListActionResult<T>(this Response<List<T>> response)
    {
        return response.StatusCode switch
        {
            HttpStatusCode.OK => new OkObjectResult(response),
            HttpStatusCode.NoContent => new NoContentResult(),
            HttpStatusCode.NotFound => new NotFoundObjectResult(response),
            HttpStatusCode.BadRequest => new BadRequestObjectResult(response),
            HttpStatusCode.Unauthorized => new UnauthorizedObjectResult(response),
            _ => new ObjectResult(response)
            {
                StatusCode = (int)response.StatusCode
            }
        };
    }
}