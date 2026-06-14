using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Enums;

namespace EventosVivos.Domain.Repositories;

public interface IReservationRepository
{
    Task<Reservation?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Reservation>> GetByEventIdAsync(Guid eventId, CancellationToken ct = default);
    Task<PagedQueryResult<Reservation>> GetFilteredPageAsync(
        Guid? eventId,
        ReservationStatus? status,
        string? buyerEmail,
        int pageNumber,
        int pageSize,
        CancellationToken ct = default);
    Task<bool> CodeExistsAsync(string code, CancellationToken ct = default);
    Task AddAsync(Reservation reservation, CancellationToken ct = default);
    Task UpdateAsync(Reservation reservation, CancellationToken ct = default);
}
