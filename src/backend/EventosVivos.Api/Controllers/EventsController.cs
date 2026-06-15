using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using EventosVivos.Application.DTOs;
using EventosVivos.Application.Features.Events.Commands.CancelEvent;
using EventosVivos.Application.Features.Events.Commands.CreateEvent;
using EventosVivos.Application.Features.Events.Commands.UpdateEvent;
using EventosVivos.Application.Features.Events.Queries.GetEventById;
using EventosVivos.Application.Features.Events.Queries.ListEventReservations;
using EventosVivos.Application.Features.Events.Queries.ListEvents;
using MediatR;

namespace EventosVivos.Api.Controllers;

/// <summary>
/// Event management: create, list, get, update, and cancel events.
/// Business rules include venue capacity limits (RN-01), overlap prevention (RN-02),
/// weekend 22:00 end-time constraint (RN-03), and derived status (activo, completado, cancelado).
/// </summary>
[ApiController]
[Route("api/v1/events")]
[SwaggerTag("Event management — create, list, get, update, and cancel events")]
public class EventsController : ControllerBase
{
    private readonly ISender _sender;

    public EventsController(ISender sender) => _sender = sender;

    /// <summary>Create a new event.</summary>
    /// <remarks>
    /// Validates business rules:
    /// <list type="bullet">
    ///   <item>Title is required (5–100 characters).</item>
    ///   <item>Description is required (10–500 characters).</item>
    ///   <item>Max capacity must not exceed the venue's total capacity (RN-01).</item>
    ///   <item>No overlapping active events in the same venue (RN-02).</item>
    ///   <item>Weekend events must end by 22:00 (RN-03).</item>
    ///   <item>Event must start in the future.</item>
    /// </list>
    /// Returns 201 with the new event ID on success.
    /// </remarks>
    /// <param name="request">Event creation payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>201 with event ID, or ProblemDetails on failure.</returns>
    [HttpPost]
    [SwaggerOperation(
        OperationId = "Events_Create",
        Summary = "Create a new event",
        Description = "Creates an event after validating venue capacity, overlap, weekend time constraints, and future-start rules. Returns the new event ID.")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateEventRequest request,
        CancellationToken ct)
    {
        var result = await _sender.Send(request, ct);
        if (result.IsFailure)
            return this.ToProblem(result);

        return CreatedAtAction(nameof(GetById), new { id = result.Data }, result.Data);
    }

    /// <summary>List and filter events with pagination.</summary>
    /// <remarks>
    /// Returns events with optional filters: by type, venue, date range, derived status, or title search.
    /// Results are paginated with stable ordering (StartsAt desc, Title asc).
    /// The <c>status</c> filter accepts derived values: <c>activo</c>, <c>completado</c>, <c>cancelado</c>.
    /// </remarks>
    /// <param name="type">Filter by event type (conferencia, taller, concierto).</param>
    /// <param name="venueId">Filter by venue ID.</param>
    /// <param name="startsAtFrom">Filter events starting at or after this date.</param>
    /// <param name="startsAtTo">Filter events starting at or before this date.</param>
    /// <param name="status">Filter by derived status (activo, completado, cancelado).</param>
    /// <param name="titleSearch">Search events by title (partial match).</param>
    /// <param name="pageNumber">Page number (1-based, default 1).</param>
    /// <param name="pageSize">Items per page (1-100, default 10).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paged list of matching events.</returns>
    [HttpGet]
    [SwaggerOperation(
        OperationId = "Events_List",
        Summary = "List / filter events",
        Description = "Returns events with optional filters: type, venue, date range, derived status, or title search. Results are paginated with stable ordering (StartsAt desc, Title asc).")]
    [ProducesResponseType(typeof(PagedResult<EventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> List(
        [FromQuery] string? type,
        [FromQuery] int? venueId,
        [FromQuery] DateTimeOffset? startsAtFrom,
        [FromQuery] DateTimeOffset? startsAtTo,
        [FromQuery] string? status,
        [FromQuery] string? titleSearch,
        CancellationToken ct,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = new ListEventsQuery(
            Type: type,
            VenueId: venueId,
            StartsAtFrom: startsAtFrom,
            StartsAtTo: startsAtTo,
            Status: status,
            TitleSearch: titleSearch,
            PageNumber: pageNumber,
            PageSize: pageSize);

        var result = await _sender.Send(query, ct);
        if (result.IsFailure)
            return this.ToProblem(result);

        return Ok(result.Data);
    }

    /// <summary>Get a single event by ID.</summary>
    /// <remarks>
    /// Returns the full event details including the derived public status (activo, completado, cancelado).
    /// </remarks>
    /// <param name="id">Event GUID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Event details with derived status.</returns>
    [HttpGet("{id:guid}")]
    [SwaggerOperation(
        OperationId = "Events_GetById",
        Summary = "Get event by ID",
        Description = "Returns the full event details with derived public status (activo, completado, cancelado).")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new GetEventQuery(id), ct);
        if (result.IsFailure)
            return this.ToProblem(result);

        return Ok(result.Data);
    }

    /// <summary>Update an existing event.</summary>
    /// <remarks>
    /// Updates mutable event fields. Business rules (capacity, overlap, weekend time) are re-validated.
    /// The overlap check excludes the current event's own time slot.
    /// Rejects updates on canceled or completed events.
    /// </remarks>
    /// <param name="id">Event GUID.</param>
    /// <param name="request">Event update payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>200 on success, or ProblemDetails on failure.</returns>
    [HttpPut("{id:guid}")]
    [SwaggerOperation(
        OperationId = "Events_Update",
        Summary = "Update event",
        Description = "Updates an existing event. Re-validates capacity, overlap, and weekend rules. Rejects updates on canceled or completed events.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateEventRequest request,
        CancellationToken ct)
    {
        var updateRequest = request with { EventId = id };
        var result = await _sender.Send(updateRequest, ct);
        if (result.IsFailure)
            return this.ToProblem(result);

        return Ok();
    }

    /// <summary>Cancel an event.</summary>
    /// <remarks>
    /// Marks the event as canceled. Once canceled, the event cannot be updated.
    /// </remarks>
    /// <param name="id">Event GUID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>200 on success, or ProblemDetails on failure.</returns>
    [HttpPost("{id:guid}/cancel")]
    [SwaggerOperation(
        OperationId = "Events_Cancel",
        Summary = "Cancel event",
        Description = "Marks an event as canceled. Canceled events cannot be updated.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new CancelEventRequest(id), ct);
        if (result.IsFailure)
            return this.ToProblem(result);

        return Ok();
    }

    /// <summary>List reservations for an event with pagination.</summary>
    /// <remarks>
    /// Returns reservations associated with a given event, with pagination support.
    /// </remarks>
    /// <param name="id">Event GUID.</param>
    /// <param name="pageNumber">Page number (1-based, default 1).</param>
    /// <param name="pageSize">Items per page (1-100, default 10).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paged list of reservations for the event.</returns>
    [HttpGet("{id:guid}/reservations")]
    [SwaggerOperation(
        OperationId = "Events_ListReservations",
        Summary = "List reservations by event",
        Description = "Returns paginated reservations for the specified event.")]
    [ProducesResponseType(typeof(PagedResult<ReservationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetReservations(
        Guid id,
        CancellationToken ct,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = new ListEventReservationsQuery(
            EventId: id,
            PageNumber: pageNumber,
            PageSize: pageSize);
        var result = await _sender.Send(query, ct);
        if (result.IsFailure)
            return this.ToProblem(result);

        return Ok(result.Data);
    }
}
