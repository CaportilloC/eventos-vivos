namespace EventosVivos.Domain.Repositories;

public sealed record PagedQueryResult<T>(IReadOnlyList<T> Items, int TotalCount);
