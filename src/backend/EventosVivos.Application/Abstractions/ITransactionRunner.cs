namespace EventosVivos.Application.Abstractions;

public interface ITransactionRunner
{
    Task<TResult> RunSerializableAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken ct = default);
}

internal sealed class NoopTransactionRunner : ITransactionRunner
{
    public Task<TResult> RunSerializableAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken ct = default) =>
        operation(ct);
}
