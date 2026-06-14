using EventosVivos.Domain;
using EventosVivos.Domain.Repositories;
using EventosVivos.Application.DTOs;

namespace EventosVivos.Application.Handlers;

/// <summary>
/// Query to get a single reservation by ID.
/// </summary>
/// <param name="ReservationId">Identifier of the reservation to retrieve.</param>
public record GetReservationQuery(Guid ReservationId);

/// <summary>
/// Handles fetching a single reservation by ID.
/// </summary>
public class GetReservationHandler
{
    private readonly IReservationRepository _reservationRepository;

    public GetReservationHandler(IReservationRepository reservationRepository)
    {
        _reservationRepository = reservationRepository;
    }

    public async Task<Result<ReservationResponse>> HandleAsync(
        GetReservationQuery query, CancellationToken ct = default)
    {
        var reservation = await _reservationRepository.GetByIdAsync(query.ReservationId, ct);
        if (reservation is null)
            return Result<ReservationResponse>.Failure(
                $"Reservation with ID {query.ReservationId} not found.", ErrorType.NotFound);

        return Result<ReservationResponse>.Success(ReservationResponseMapper.FromReservation(reservation));
    }
}
