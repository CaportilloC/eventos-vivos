using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using EventosVivos.Application.Common.Behaviors;

namespace EventosVivos.Application;

/// <summary>
/// Application-layer DI registration.
/// Registers application handlers, pipeline behaviors, and FluentValidation validators.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<ApplicationAssembly>();
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssemblyContaining<ApplicationAssembly>();

        return services;
    }
}
