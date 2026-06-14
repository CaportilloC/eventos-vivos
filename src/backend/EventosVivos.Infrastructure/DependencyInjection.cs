using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using EventosVivos.Application.Abstractions;
using EventosVivos.Domain.Repositories;
using EventosVivos.Domain.Services;
using EventosVivos.Infrastructure.Data;
using EventosVivos.Infrastructure.Repositories;
using EventosVivos.Infrastructure.Services;

namespace EventosVivos.Infrastructure;

/// <summary>
/// Infrastructure-layer DI registration.
/// Registers DbContext, EF Core repositories, and SystemClock.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<EventosVivosDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IVenueRepository, VenueRepository>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IReservationRepository, ReservationRepository>();
        services.AddScoped<ITransactionRunner, EfTransactionRunner>();

        services.AddSingleton<IClock, SystemClock>();

        return services;
    }
}
