namespace EventosVivos.Application.DTOs;

/// <summary>
/// Generic paged result for list endpoints.
/// Provides pagination metadata alongside the items for the current page.
/// </summary>
/// <typeparam name="T">The type of items in the page.</typeparam>
/// <param name="Items">The items for the current page.</param>
/// <param name="PageNumber">Current page number (1-based).</param>
/// <param name="PageSize">Number of items per page.</param>
/// <param name="TotalCount">Total number of items across all pages.</param>
/// <param name="TotalPages">Total number of pages.</param>
/// <param name="HasPreviousPage">Whether a previous page exists.</param>
/// <param name="HasNextPage">Whether a next page exists.</param>
public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage);
