using System.Data;
using EventosVivos.Application.Abstractions;
using EventosVivos.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EventosVivos.Infrastructure.Services;

public sealed class EfTransactionRunner : ITransactionRunner
{
    private readonly EventosVivosDbContext _db;

    public EfTransactionRunner(EventosVivosDbContext db) => _db = db;

    public async Task<TResult> RunSerializableAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken ct = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var result = await operation(ct);
        await transaction.CommitAsync(ct);
        return result;
    }
}
