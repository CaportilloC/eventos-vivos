using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using EventosVivos.Application.DTOs;
using EventosVivos.Application.Features.Reports.Queries.GetOccupancyReport;
using MediatR;

namespace EventosVivos.Api.Controllers;

/// <summary>
/// Reports and analytics: per-event occupancy metrics, revenue, and availability.
/// </summary>
[ApiController]
[Route("api/v1/events")]
[SwaggerTag("Reports — occupancy metrics and analytics per event")]
public class ReportsController : ControllerBase
{
    private readonly ISender _sender;

    public ReportsController(ISender sender) => _sender = sender;

    /// <summary>Get occupancy report by event.</summary>
    /// <remarks>
    /// Returns occupancy metrics for a specific event:
    /// <list type="bullet">
    ///   <item><c>confirmedTickets</c> — tickets with confirmed payment.</item>
    ///   <item><c>lostTickets</c> — tickets lost due to late cancellation (perdida).</item>
    ///   <item><c>availableTickets</c> — remaining capacity (after confirmed + lost + active pending holds).</item>
    ///   <item><c>occupancyPercentage</c> — confirmed tickets as percentage of max capacity.</item>
    ///   <item><c>revenue</c> — total revenue from confirmed tickets (price × confirmed count).</item>
    /// </list>
    /// </remarks>
    /// <param name="id">Event GUID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Occupancy report for the event.</returns>
    [HttpGet("{id:guid}/occupancy-report")]
    [SwaggerOperation(
        OperationId = "Reports_GetOccupancyByEvent",
        Summary = "Get occupancy report by event",
        Description = "Returns occupancy metrics: confirmed/lost/available tickets, occupancy percentage, and total revenue for a specific event.")]
    [ProducesResponseType(typeof(OccupancyReportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOccupancyReport(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new GetOccupancyReportQuery(id), ct);
        if (result.IsFailure)
            return this.ToProblem(result);

        return Ok(result.Data);
    }
}
