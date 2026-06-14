using EventosVivos.Domain;
using EventosVivos.Domain.Enums;
using EventosVivos.Domain.Repositories;
using EventosVivos.Domain.Rules;
using EventosVivos.Application.DTOs;
using MediatR;

namespace EventosVivos.Application.Features.Reservations.Queries.ListReservations;

/// <summary>
/// Query to list reservations with optional filters and pagination.
/// </summary>
/// <param name="EventId">Filter by event ID.</param>
/// <param name="Status">Filter by status (pendiente_pago, confirmada, cancelada, perdida).</param>
/// <param name="BuyerEmail">Filter by buyer email (partial match).</param>
/// <param name="PageNumber">Page number (1-based, default 1).</param>
/// <param name="PageSize">Items per page (1-100, default 10).</param>
public record ListReservationsQuery(
    Guid? EventId = null,
    string? Status = null,
    string? BuyerEmail = null,
    int PageNumber = PaginationRules.DefaultPageNumber,
    int PageSize = PaginationRules.DefaultPageSize) : IRequest<Result<PagedResult<ReservationResponse>>>;

/// <summary>
/// Handles listing reservations with optional filters and pagination.
/// Supports filtering by eventId, status, and buyerEmail.
/// Ordered by CreatedAt desc for stable pagination.
/// </summary>
public class ListReservationsHandler : IRequestHandler<ListReservationsQuery, Result<PagedResult<ReservationResponse>>>
{
    private readonly IReservationRepository _reservationRepository;

    public ListReservationsHandler(IReservationRepository reservationRepository)
    {
        _reservationRepository = reservationRepository;
    }

    public async Task<Result<PagedResult<ReservationResponse>>> Handle(
        ListReservationsQuery query, CancellationToken ct = default)
    {
        // Validate pagination parameters
        if (query.PageNumber < 1)
            return Result<PagedResult<ReservationResponse>>.Failure("PageNumber must be 1 or greater.", ErrorType.Validation);
        if (query.PageSize < 1 || query.PageSize > PaginationRules.MaxPageSize)
            return Result<PagedResult<ReservationResponse>>.Failure("PageSize must be between 1 and 100.", ErrorType.Validation);

        // Parse status filter (supports snake_case e.g. "pendiente_pago" and PascalCase)
        ReservationStatus? parsedStatus = null;
        var normalizedStatus = StatusStringParser.NormalizeToPascalCase(query.Status);
        if (!string.IsNullOrWhiteSpace(normalizedStatus))
        {
            if (!Enum.TryParse<ReservationStatus>(normalizedStatus, ignoreCase: true, out var s))
                return Result<PagedResult<ReservationResponse>>.Failure(
                    $"Invalid status '{query.Status}'. Use pendiente_pago, confirmada, cancelada, or perdida.", ErrorType.Validation);
            parsedStatus = s;
        }

        var page = await _reservationRepository.GetFilteredPageAsync(
            eventId: query.EventId,
            status: parsedStatus,
            buyerEmail: query.BuyerEmail,
            pageNumber: query.PageNumber,
            pageSize: query.PageSize,
            ct: ct);

        var totalCount = page.TotalCount;
        var totalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize);
        if (totalPages == 0) totalPages = 1;

        var pagedItems = page.Items
            .Select(ReservationResponseMapper.FromReservation)
            .ToList()
            .AsReadOnly();

        return Result<PagedResult<ReservationResponse>>.Success(new PagedResult<ReservationResponse>(
            pagedItems,
            query.PageNumber,
            query.PageSize,
            totalCount,
            totalPages,
            query.PageNumber > 1,
            query.PageNumber < totalPages));
    }
}
