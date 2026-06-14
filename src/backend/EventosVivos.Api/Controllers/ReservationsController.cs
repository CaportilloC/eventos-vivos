using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using EventosVivos.Application.DTOs;
using EventosVivos.Application.Handlers;

namespace EventosVivos.Api.Controllers;

/// <summary>
/// Reservation lifecycle: create pending reservations, confirm payment, cancel,
/// edit pending ones, and list/filter all reservations.
/// Business rules include availability (pending holds, 1h–24h time limits, price limits),
/// the 48-hour late-cancellation penalty (RN-07), and EV code generation on confirmation.
/// </summary>
[ApiController]
[Route("api/v1/reservations")]
[SwaggerTag("Reservation lifecycle — create, confirm, cancel, update, and list reservations")]
public class ReservationsController : ControllerBase
{
    private readonly ReserveTicketsHandler _reserveHandler;
    private readonly ConfirmPaymentHandler _confirmHandler;
    private readonly CancelReservationHandler _cancelHandler;
    private readonly ListReservationsHandler _listHandler;
    private readonly GetReservationHandler _getHandler;
    private readonly UpdateReservationHandler _updateHandler;

    public ReservationsController(
        ReserveTicketsHandler reserveHandler,
        ConfirmPaymentHandler confirmHandler,
        CancelReservationHandler cancelHandler,
        ListReservationsHandler listHandler,
        GetReservationHandler getHandler,
        UpdateReservationHandler updateHandler)
    {
        _reserveHandler = reserveHandler;
        _confirmHandler = confirmHandler;
        _cancelHandler = cancelHandler;
        _listHandler = listHandler;
        _getHandler = getHandler;
        _updateHandler = updateHandler;
    }

    /// <summary>Create a pending reservation.</summary>
    /// <remarks>
    /// Creates a reservation with status <c>pendiente_pago</c> (pending payment).
    /// Business rules:
    /// <list type="bullet">
    ///   <item>Buyer name is required (2–100 characters).</item>
    ///   <item>Buyer email must be a valid format.</item>
    ///   <item>Quantity must be at least 1.</item>
    ///   <item>Available capacity is checked: confirmed + lost + active pending holds are deducted.</item>
    ///   <item>Time and price limits from reservation policy are enforced.</item>
    /// </list>
    /// The reservation's <c>code</c> is null until payment is confirmed.
    /// </remarks>
    /// <param name="request">Reservation creation payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>201 with reservation details, or ProblemDetails on failure.</returns>
    [HttpPost]
    [SwaggerOperation(
        OperationId = "Reservations_Create",
        Summary = "Create pending reservation",
        Description = "Creates a pending-payment reservation after validating availability, buyer info, and time/price limits. The EV code is null until payment is confirmed.")]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reserve(
        [FromBody] ReserveTicketsRequest request,
        CancellationToken ct)
    {
        var result = await _reserveHandler.HandleAsync(request, ct);
        if (result.IsFailure)
            return this.ToProblem(result);

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data);
    }

    /// <summary>List / filter reservations with pagination.</summary>
    /// <remarks>
    /// Returns reservations with optional filters: by event, status, or buyer email.
    /// Results are paginated with stable ordering (CreatedAt desc).
    /// Status values: <c>pendiente_pago</c>, <c>confirmada</c>, <c>cancelada</c>, <c>perdida</c>.
    /// </remarks>
    /// <param name="eventId">Filter by event ID.</param>
    /// <param name="status">Filter by status (pendiente_pago, confirmada, cancelada, perdida).</param>
    /// <param name="buyerEmail">Filter by buyer email (partial match).</param>
    /// <param name="pageNumber">Page number (1-based, default 1).</param>
    /// <param name="pageSize">Items per page (1-50, default 10).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paged list of matching reservations.</returns>
    [HttpGet]
    [SwaggerOperation(
        OperationId = "Reservations_List",
        Summary = "List / filter reservations",
        Description = "Returns reservations with optional filters: event, status, or buyer email. Results are paginated with stable ordering (CreatedAt desc).")]
    [ProducesResponseType(typeof(PagedResult<ReservationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> List(
        [FromQuery] Guid? eventId,
        [FromQuery] string? status,
        [FromQuery] string? buyerEmail,
        CancellationToken ct,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = new ListReservationsQuery(
            EventId: eventId,
            Status: status,
            BuyerEmail: buyerEmail,
            PageNumber: pageNumber,
            PageSize: pageSize);

        var result = await _listHandler.HandleAsync(query, ct);
        if (result.IsFailure)
            return this.ToProblem(result);

        return Ok(result.Data);
    }

    /// <summary>Get a single reservation by ID.</summary>
    /// <remarks>
    /// Returns full reservation details including status, buyer info, timestamps, and EV code (if confirmed).
    /// </remarks>
    /// <param name="id">Reservation GUID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Reservation details.</returns>
    [HttpGet("{id:guid}")]
    [SwaggerOperation(
        OperationId = "Reservations_GetById",
        Summary = "Get reservation by ID",
        Description = "Returns the full reservation details including status, buyer info, and EV code (if confirmed).")]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _getHandler.HandleAsync(new GetReservationQuery(id), ct);
        if (result.IsFailure)
            return this.ToProblem(result);

        return Ok(result.Data);
    }

    /// <summary>Confirm payment and generate EV code.</summary>
    /// <remarks>
    /// Transitions a reservation from <c>pendiente_pago</c> to <c>confirmada</c>.
    /// A unique EV code in format <c>EV-{6 digits}</c> is generated and assigned.
    /// The code is returned in the response and is used for access control.
    /// Only pending-payment reservations can be confirmed.
    /// </remarks>
    /// <param name="id">Reservation GUID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Updated reservation with EV code, or ProblemDetails on failure.</returns>
    [HttpPost("{id:guid}/confirm-payment")]
    [SwaggerOperation(
        OperationId = "Reservations_ConfirmPayment",
        Summary = "Confirm payment and generate EV code",
        Description = "Confirms payment for a pending reservation. Generates a unique EV-{6 digits} code. Only pendiente_pago reservations can be confirmed.")]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ConfirmPayment(Guid id, CancellationToken ct)
    {
        var result = await _confirmHandler.HandleAsync(new ConfirmPaymentRequest(id), ct);
        if (result.IsFailure)
            return this.ToProblem(result);

        return Ok(result.Data);
    }

    /// <summary>Cancel a reservation.</summary>
    /// <remarks>
    /// RF-05 cancellation behavior depends on the current status:
    /// <list type="bullet">
    ///   <item><c>pendiente_pago</c>: immediately canceled, seats released.</item>
    ///   <item><c>confirmada</c>: uses the approved 48-hour rule. If canceled less than 48h before the event start, it becomes <c>perdida</c> and seats are NOT released. If 48h or more before, it becomes <c>cancelada</c> and seats are released.</item>
    ///   <item><c>cancelada</c> and <c>perdida</c>: rejected.</item>
    /// </list>
    /// </remarks>
    /// <param name="id">Reservation GUID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Updated reservation after cancellation.</returns>
    [HttpPost("{id:guid}/cancel")]
    [SwaggerOperation(
        OperationId = "Reservations_Cancel",
        Summary = "Cancel reservation",
        Description = "Cancels a reservation. Pending: immediate cancel, seats released. Confirmed: approved 48h rule applies; less than 48h becomes perdida without releasing seats, 48h+ becomes cancelada and releases seats. Cancelada/perdida are rejected.")]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var result = await _cancelHandler.HandleAsync(new CancelReservationRequest(id), ct);
        if (result.IsFailure)
            return this.ToProblem(result);

        return Ok(result.Data);
    }

    /// <summary>Update a pending reservation.</summary>
    /// <remarks>
    /// Updates the buyer name, email, and ticket quantity for a pending-payment reservation.
    /// Only reservations with status <c>pendiente_pago</c> can be edited.
    /// Availability is re-checked, excluding the current reservation's held seats from the count.
    /// </remarks>
    /// <param name="id">Reservation GUID.</param>
    /// <param name="request">Reservation update payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Updated reservation, or ProblemDetails on failure.</returns>
    [HttpPut("{id:guid}")]
    [SwaggerOperation(
        OperationId = "Reservations_Update",
        Summary = "Update pending reservation",
        Description = "Updates buyer info and quantity for a pendiente_pago reservation. Re-checks availability excluding current held seats. Rejects edits on confirmed/canceled/lost reservations.")]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateReservationRequest request,
        CancellationToken ct)
    {
        if (id != request.ReservationId)
            return Problem(statusCode: 400, detail: "ID mismatch between route and request body.");

        var result = await _updateHandler.HandleAsync(request, ct);
        if (result.IsFailure)
            return this.ToProblem(result);

        return Ok(result.Data);
    }
}
