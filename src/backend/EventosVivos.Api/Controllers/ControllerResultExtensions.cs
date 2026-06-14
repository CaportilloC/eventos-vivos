using EventosVivos.Domain;
using Microsoft.AspNetCore.Mvc;

namespace EventosVivos.Api.Controllers;

internal static class ControllerResultExtensions
{
    public static IActionResult ToProblem(this ControllerBase controller, Result result)
    {
        var statusCode = result.ErrorType switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        return controller.Problem(statusCode: statusCode, detail: result.Error);
    }
}
