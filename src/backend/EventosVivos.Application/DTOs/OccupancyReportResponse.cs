namespace EventosVivos.Application.DTOs;

/// <summary>
/// Per-event occupancy and revenue report.
/// </summary>
/// <param name="EventId">Event identifier.</param>
/// <param name="Status">Derived event status (activo, completado, cancelado).</param>
/// <param name="ConfirmedTickets">Number of tickets with confirmed payment (included in revenue).</param>
/// <param name="LostTickets">Tickets lost due to late cancellation (perdida) — these do NOT release capacity.</param>
/// <param name="AvailableTickets">Remaining capacity after confirmed + lost + active pending holds are deducted.</param>
/// <param name="OccupancyPercentage">Percentage of max capacity occupied by confirmed tickets (0–100%).</param>
/// <param name="Revenue">Total revenue from confirmed tickets (price × confirmed tickets).</param>
public record OccupancyReportResponse(
    Guid EventId,
    string Status,
    int ConfirmedTickets,
    int LostTickets,
    int AvailableTickets,
    decimal OccupancyPercentage,
    decimal Revenue);
