using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using EventosVivos.Application.Handlers;

namespace EventosVivos.Application;

/// <summary>
/// Application-layer DI registration.
/// Registers all command/query handlers and FluentValidation validators.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register handlers as scoped services
        services.AddScoped<CreateEventHandler>();
        services.AddScoped<ReserveTicketsHandler>();
        services.AddScoped<ConfirmPaymentHandler>();
        services.AddScoped<CancelReservationHandler>();
        services.AddScoped<CancelEventHandler>();
        services.AddScoped<ListEventsHandler>();
        services.AddScoped<GetOccupancyReportHandler>();
        services.AddScoped<GetEventHandler>();
        services.AddScoped<UpdateEventHandler>();
        services.AddScoped<ListReservationsHandler>();
        services.AddScoped<GetReservationHandler>();
        services.AddScoped<UpdateReservationHandler>();

        // Register FluentValidation validators
        services.AddValidatorsFromAssemblyContaining<ApplicationAssembly>();

        return services;
    }
}
