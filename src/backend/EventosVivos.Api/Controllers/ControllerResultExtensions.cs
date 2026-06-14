using EventosVivos.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

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

        return controller.ToProblem(statusCode, result.Error, result.ErrorType.ToString());
    }

    public static IActionResult ToProblem(
        this ControllerBase controller,
        int statusCode,
        string? detail,
        string errorCode)
    {
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Detail = detail,
            Title = ReasonPhrases.GetReasonPhrase(statusCode),
            Type = $"https://httpstatuses.com/{statusCode}",
            Instance = controller.HttpContext.Request.Path
        };

        problem.Extensions["traceId"] = controller.HttpContext.TraceIdentifier;
        problem.Extensions["errorCode"] = errorCode;

        return controller.StatusCode(statusCode, problem);
    }
}
