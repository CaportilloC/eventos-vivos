namespace EventosVivos.Application.DTOs;

/// <summary>
/// Public representation of an event for list/detail responses.
/// </summary>
/// <param name="Id">Event unique identifier.</param>
/// <param name="Title">Event title (5–100 characters).</param>
/// <param name="Description">Event description (10–500 characters).</param>
/// <param name="Type">Event type: conferencia, taller, or concierto.</param>
/// <param name="VenueId">Identifier of the venue where the event takes place.</param>
/// <param name="Status">Derived public status: activo, completado, or cancelado.
/// <c>activo</c> = currently happening or upcoming;
/// <c>completado</c> = event end time has passed;
/// <c>cancelado</c> = event was manually canceled.</param>
/// <param name="StartsAt">Event start date/time.</param>
/// <param name="EndsAt">Event end date/time. Weekend events must end by 22:00 (RN-03).</param>
/// <param name="Price">Ticket price in COP. Must be greater than zero.</param>
/// <param name="MaxCapacity">Maximum tickets available. Cannot exceed venue capacity (RN-01).</param>
public record EventResponse(
    Guid Id,
    string Title,
    string? Description,
    string Type,
    int VenueId,
    string Status,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    decimal Price,
    int MaxCapacity);

/// <summary>
/// Minimal event representation used by reservation forms.
/// </summary>
public record AvailableReservationEventResponse(
    Guid Id,
    string Title,
    DateTimeOffset StartsAt,
    decimal Price,
    int MaxCapacity,
    int OccupiedTickets,
    int AvailableTickets,
    string Status);
