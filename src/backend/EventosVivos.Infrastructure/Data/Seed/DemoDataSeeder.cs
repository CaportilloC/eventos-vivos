using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Enums;
using EventosVivos.Domain.Services;
using EventosVivos.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EventosVivos.Infrastructure.Data.Seed;

public static class DemoDataSeeder
{
    private const string DemoTitlePrefix = "Demo | ";

    public static async Task SeedAsync(
        EventosVivosDbContext db,
        IClock clock,
        DemoDataOptions options,
        ILogger logger,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        if (options.ResetBeforeSeed)
        {
            await db.Reservations.ExecuteDeleteAsync(ct);
            await db.Events.ExecuteDeleteAsync(ct);
        }
        else if (await db.Events.AnyAsync(e => e.Title.StartsWith(DemoTitlePrefix), ct))
        {
            logger.LogInformation("Demo data already exists; skipping seed.");
            return;
        }

        var venues = await db.Venues
            .Where(v => v.Id == 1 || v.Id == 2 || v.Id == 3)
            .ToDictionaryAsync(v => v.Id, ct);

        if (venues.Count != 3)
            throw new InvalidOperationException("Demo data requires venues with IDs 1, 2, and 3.");

        var nowBogota = ColombiaTime.NowInColombia(clock);

        var events = new[]
        {
            CreateEvent("Cumbre de Innovacion Empresarial", EventType.Conferencia, 1, 180, 120000m, PastSchedule(nowBogota, 21, 18), "Conferencia ejecutiva sobre tecnologia, liderazgo y crecimiento regional."),
            CreateEvent("Laboratorio de Producto Digital", EventType.Taller, 1, 120, 85000m, CurrentSchedule(nowBogota), "Taller practico para equipos que construyen productos digitales."),
            CreateEvent("Noche Sinfonica Urbana", EventType.Concierto, 1, 180, 180000m, FutureSchedule(nowBogota, 7, 19), "Concierto de fusion orquestal y sonidos urbanos para publico general."),

            CreateEvent("Taller de Finanzas para Pymes", EventType.Taller, 2, 45, 45000m, PastSchedule(nowBogota, 18, 17), "Sesion aplicada de planeacion financiera para pequenas empresas."),
            CreateEvent("Acustico Andino en Vivo", EventType.Concierto, 2, 45, 85000m, CurrentSchedule(nowBogota), "Formato intimo con repertorio andino contemporaneo y artistas invitados."),
            CreateEvent("Foro Futuro del Turismo", EventType.Conferencia, 2, 45, 120000m, FutureSchedule(nowBogota, 8, 18), "Encuentro para analizar tendencias del turismo sostenible en Colombia."),

            CreateEvent("Festival Altavoz Medellin", EventType.Concierto, 3, 450, 180000m, PastSchedule(nowBogota, 14, 19), "Festival musical con bandas nacionales, invitados y experiencia gastronomica."),
            CreateEvent("Congreso Colombia Sostenible", EventType.Conferencia, 3, 300, 120000m, CurrentSchedule(nowBogota), "Congreso empresarial sobre sostenibilidad, energia y ciudades inteligentes."),
            CreateEvent("Bootcamp de Marketing Cultural", EventType.Taller, 3, 220, 85000m, FutureSchedule(nowBogota, 9, 18), "Entrenamiento intensivo para promocionar proyectos culturales con datos."),
        };

        foreach (var @event in events)
        {
            var venueCapacity = venues[@event.VenueId].Capacity;
            if (@event.MaxCapacity > venueCapacity)
                throw new InvalidOperationException($"Event '{@event.Title}' exceeds venue capacity.");
        }

        await db.Events.AddRangeAsync(events, ct);

        var codeSequence = 100001;
        var reservations = new List<Reservation>();

        AddPastStory(reservations, events[0], nowBogota, ref codeSequence, 150, 10, 8);
        AddPastStory(reservations, events[3], nowBogota, ref codeSequence, 40, 2, 3);
        AddPastStory(reservations, events[6], nowBogota, ref codeSequence, 410, 10, 15);

        AddCurrentStory(reservations, events[1], nowBogota, ref codeSequence, 70, 20, 5);
        AddCurrentStory(reservations, events[4], nowBogota, ref codeSequence, 20, 10, 3);
        AddCurrentStory(reservations, events[7], nowBogota, ref codeSequence, 160, 50, 12);

        AddFutureStory(reservations, events[2], nowBogota, ref codeSequence, 45, 18, 4);
        AddFutureStory(reservations, events[5], nowBogota, ref codeSequence, 12, 8, 2);
        AddFutureStory(reservations, events[8], nowBogota, ref codeSequence, 65, 25, 6);

        await db.Reservations.AddRangeAsync(reservations, ct);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Seeded demo data with {EventCount} events and {ReservationCount} reservations.",
            events.Length,
            reservations.Count);
    }

    private static Event CreateEvent(
        string title,
        EventType type,
        int venueId,
        int maxCapacity,
        decimal price,
        EventSchedule schedule,
        string description) =>
        new($"{DemoTitlePrefix}{title}", type, venueId, maxCapacity, new Money(price), schedule, description);

    private static EventSchedule PastSchedule(DateTimeOffset nowBogota, int daysAgo, int hour) =>
        ScheduleAt(nowBogota.Date.AddDays(-daysAgo), hour, 0, TimeSpan.FromHours(3), nowBogota.Offset);

    private static EventSchedule FutureSchedule(DateTimeOffset nowBogota, int daysAhead, int hour) =>
        ScheduleAt(nowBogota.Date.AddDays(daysAhead), hour, 0, TimeSpan.FromHours(3), nowBogota.Offset);

    private static EventSchedule CurrentSchedule(DateTimeOffset nowBogota)
    {
        var startsAt = nowBogota.AddHours(-1);
        if (startsAt.Hour > 22 || nowBogota.Hour < 2)
        {
            var date = nowBogota.Hour < 2 ? nowBogota.Date.AddDays(-1) : nowBogota.Date;
            startsAt = AtBogota(date, 21, 0, nowBogota.Offset);
        }

        return new EventSchedule(startsAt, nowBogota.AddHours(3));
    }

    private static EventSchedule ScheduleAt(
        DateTime date,
        int hour,
        int minute,
        TimeSpan duration,
        TimeSpan offset)
    {
        var startsAt = AtBogota(date, hour, minute, offset);
        return new EventSchedule(startsAt, startsAt.Add(duration));
    }

    private static DateTimeOffset AtBogota(DateTime date, int hour, int minute, TimeSpan offset) =>
        new(date.Year, date.Month, date.Day, hour, minute, 0, offset);

    private static void AddPastStory(
        List<Reservation> reservations,
        Event @event,
        DateTimeOffset nowBogota,
        ref int codeSequence,
        int confirmedTickets,
        int lostTickets,
        int canceledTickets)
    {
        reservations.Add(Confirmed(@event, "Laura Gomez", "laura.gomez@example.com", confirmedTickets, @event.Schedule.StartsAt.AddDays(-18), ref codeSequence));
        reservations.Add(Lost(@event, "Carlos Ramirez", "carlos.ramirez@example.com", lostTickets, @event.Schedule.StartsAt.AddDays(-12), @event.Schedule.StartsAt.AddHours(-6), ref codeSequence));
        reservations.Add(CanceledConfirmed(@event, "Natalia Torres", "natalia.torres@example.com", canceledTickets, @event.Schedule.StartsAt.AddDays(-10), @event.Schedule.StartsAt.AddDays(-4), ref codeSequence));
    }

    private static void AddCurrentStory(
        List<Reservation> reservations,
        Event @event,
        DateTimeOffset nowBogota,
        ref int codeSequence,
        int confirmedTickets,
        int pendingTickets,
        int lostTickets)
    {
        reservations.Add(Confirmed(@event, "Andres Mejia", "andres.mejia@example.com", confirmedTickets, nowBogota.AddDays(-5), ref codeSequence));
        reservations.Add(Pending(@event, "Valentina Rojas", "valentina.rojas@example.com", pendingTickets, nowBogota.AddMinutes(-5)));
        reservations.Add(Lost(@event, "Miguel Herrera", "miguel.herrera@example.com", lostTickets, nowBogota.AddDays(-2), @event.Schedule.StartsAt.AddHours(-8), ref codeSequence));
    }

    private static void AddFutureStory(
        List<Reservation> reservations,
        Event @event,
        DateTimeOffset nowBogota,
        ref int codeSequence,
        int confirmedTickets,
        int pendingTickets,
        int canceledTickets)
    {
        reservations.Add(Confirmed(@event, "Paula Castro", "paula.castro@example.com", confirmedTickets, nowBogota.AddDays(-2), ref codeSequence));
        reservations.Add(Pending(@event, "Santiago Ruiz", "santiago.ruiz@example.com", pendingTickets, nowBogota.AddMinutes(-4)));
        reservations.Add(CanceledConfirmed(@event, "Diana Moreno", "diana.moreno@example.com", canceledTickets, nowBogota.AddDays(-3), nowBogota.AddDays(-1), ref codeSequence));
    }

    private static Reservation Pending(Event @event, string name, string email, int quantity, DateTimeOffset createdAt) =>
        new(@event.Id, new Buyer(name, email), quantity, createdAt);

    private static Reservation Confirmed(
        Event @event,
        string name,
        string email,
        int quantity,
        DateTimeOffset createdAt,
        ref int codeSequence)
    {
        var reservation = Pending(@event, name, email, quantity, createdAt);
        reservation.Confirm(NextCode(ref codeSequence), createdAt.AddMinutes(5));
        return reservation;
    }

    private static Reservation Lost(
        Event @event,
        string name,
        string email,
        int quantity,
        DateTimeOffset createdAt,
        DateTimeOffset canceledAt,
        ref int codeSequence)
    {
        var reservation = Confirmed(@event, name, email, quantity, createdAt, ref codeSequence);
        reservation.CancelConfirmed(releaseSeats: false, canceledAt);
        return reservation;
    }

    private static Reservation CanceledConfirmed(
        Event @event,
        string name,
        string email,
        int quantity,
        DateTimeOffset createdAt,
        DateTimeOffset canceledAt,
        ref int codeSequence)
    {
        var reservation = Confirmed(@event, name, email, quantity, createdAt, ref codeSequence);
        reservation.CancelConfirmed(releaseSeats: true, canceledAt);
        return reservation;
    }

    private static string NextCode(ref int codeSequence) => $"EV-{codeSequence++:D6}";
}
