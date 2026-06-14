using EventosVivos.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EventosVivos.Application.Common.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        _logger.LogInformation("Handling application request {RequestName}", requestName);

        try
        {
            var response = await next(cancellationToken);
            if (response is Result { IsFailure: true } result)
            {
                _logger.LogWarning(
                    "Application request {RequestName} failed with {ErrorType}: {Error}",
                    requestName,
                    result.ErrorType,
                    result.Error);
            }
            else
            {
                _logger.LogInformation("Handled application request {RequestName}", requestName);
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Application request {RequestName} threw an unexpected exception", requestName);
            throw;
        }
    }
}
