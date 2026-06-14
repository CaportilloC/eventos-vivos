using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using EventosVivos.Application.DTOs;
using EventosVivos.Domain.Entities;
using EventosVivos.Application.Features.Venues.Queries.ListVenues;
using MediatR;

namespace EventosVivos.Api.Controllers;

/// <summary>
/// Venue reference data: list the seeded venues used for event scheduling.
/// Each venue has a fixed capacity that limits event max capacity (RN-01).
/// </summary>
[ApiController]
[Route("api/v1/venues")]
[SwaggerTag("Venues — reference data for event scheduling")]
public class VenuesController : ControllerBase
{
    private readonly ISender _sender;

    public VenuesController(ISender sender) => _sender = sender;

    /// <summary>List all venues with pagination.</summary>
    /// <remarks>
    /// Returns the seeded venue list with pagination. Each venue has a name, address, and capacity.
    /// Capacity determines the maximum tickets available for events at this venue (RN-01).
    /// Ordered by Id asc for stable pagination.
    /// </remarks>
    /// <param name="pageNumber">Page number (1-based, default 1).</param>
    /// <param name="pageSize">Items per page (1-50, default 50 to serve dropdowns).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paged list of all venues.</returns>
    [HttpGet]
    [SwaggerOperation(
        OperationId = "Venues_List",
        Summary = "List venues",
        Description = "Returns all seeded venues with pagination. Default pageSize is 50 to serve reference-data dropdowns. Ordered by Id asc for stable pagination.")]
    [ProducesResponseType(typeof(PagedResult<Venue>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll(
        CancellationToken ct,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await _sender.Send(new ListVenuesQuery(pageNumber, pageSize), ct);
        if (result.IsFailure)
            return this.ToProblem(result);

        return Ok(result.Data);
    }
}
