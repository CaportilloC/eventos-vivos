using EventosVivos.Domain;
using FluentValidation;
using MediatR;

namespace EventosVivos.Application.Common.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next(cancellationToken);

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
            _validators.Select(validator => validator.ValidateAsync(context, cancellationToken)));

        var errors = validationResults
            .SelectMany(result => result.Errors)
            .Where(error => error is not null)
            .Select(error => error.ErrorMessage)
            .Distinct()
            .ToArray();

        if (errors.Length == 0)
            return await next(cancellationToken);

        var message = string.Join(" ", errors);
        if (TryCreateFailure(message, out var response))
            return response;

        throw new ValidationException(message);
    }

    private static bool TryCreateFailure(string error, out TResponse response)
    {
        response = default!;
        var responseType = typeof(TResponse);

        if (!typeof(Result).IsAssignableFrom(responseType))
            return false;

        var failure = responseType.GetMethod(
            nameof(Result.Failure),
            [typeof(string), typeof(ErrorType)]);
        if (failure is null)
            return false;

        response = (TResponse)failure.Invoke(null, [error, ErrorType.Validation])!;
        return true;
    }
}
