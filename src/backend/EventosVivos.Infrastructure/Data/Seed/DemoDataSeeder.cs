using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Enums;
using EventosVivos.Domain.Services;
using EventosVivos.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EventosVivos.Infrastructure.Data.Seed;

public static class DemoDataSeeder
{
    private static readonly SeedBuyer[] Buyers =
    [
        new("Laura Gomez", "laura.gomez@example.com"),
        new("Carlos Ramirez", "carlos.ramirez@example.com"),
        new("Natalia Torres", "natalia.torres@example.com"),
        new("Andres Mejia", "andres.mejia@example.com"),
        new("Valentina Rojas", "valentina.rojas@example.com"),
        new("Miguel Herrera", "miguel.herrera@example.com"),
        new("Paula Castro", "paula.castro@example.com"),
        new("Santiago Ruiz", "santiago.ruiz@example.com"),
        new("Diana Moreno", "diana.moreno@example.com"),
        new("Camila Restrepo", "camila.restrepo@example.com"),
        new("Juan Pablo Ospina", "juan.ospina@example.com"),
        new("Manuela Arias", "manuela.arias@example.com"),
        new("Felipe Cardenas", "felipe.cardenas@example.com"),
        new("Isabella Montoya", "isabella.montoya@example.com"),
        new("Sebastian Quintero", "sebastian.quintero@example.com"),
        new("Daniela Giraldo", "daniela.giraldo@example.com"),
        new("Mateo Salazar", "mateo.salazar@example.com"),
        new("Mariana Valencia", "mariana.valencia@example.com"),
        new("Julian Pineda", "julian.pineda@example.com"),
        new("Sofia Bernal", "sofia.bernal@example.com"),
        new("Alejandro Vargas", "alejandro.vargas@example.com"),
        new("Luisa Fernanda Diaz", "luisa.diaz@example.com"),
        new("Nicolas Cifuentes", "nicolas.cifuentes@example.com"),
        new("Tatiana Acosta", "tatiana.acosta@example.com"),
        new("Esteban Rios", "esteban.rios@example.com"),
        new("Carolina Medina", "carolina.medina@example.com"),
        new("Jorge Andres Franco", "jorge.franco@example.com"),
        new("Maria Jose Lopez", "maria.lopez@example.com"),
        new("David Camacho", "david.camacho@example.com"),
        new("Ana Maria Ceballos", "ana.ceballos@example.com"),
        new("Cristian Bedoya", "cristian.bedoya@example.com"),
        new("Sara Pulido", "sara.pulido@example.com"),
        new("Oscar Villamizar", "oscar.villamizar@example.com"),
        new("Catalina Duarte", "catalina.duarte@example.com"),
        new("Ricardo Benitez", "ricardo.benitez@example.com"),
        new("Veronica Mesa", "veronica.mesa@example.com"),
        new("Hernan Castillo", "hernan.castillo@example.com"),
        new("Monica Villalba", "monica.villalba@example.com"),
        new("Tomas Prieto", "tomas.prieto@example.com"),
        new("Adriana Pardo", "adriana.pardo@example.com"),
        new("Kevin Munoz", "kevin.munoz@example.com"),
        new("Juliana Robledo", "juliana.robledo@example.com"),
        new("Diego Forero", "diego.forero@example.com"),
        new("Marcela Guzman", "marcela.guzman@example.com"),
        new("Rafael Carvajal", "rafael.carvajal@example.com"),
        new("Gabriela Naranjo", "gabriela.naranjo@example.com"),
        new("Emilio Andrade", "emilio.andrade@example.com"),
        new("Lorena Caceres", "lorena.caceres@example.com"),
        new("Mauricio Gallego", "mauricio.gallego@example.com"),
        new("Viviana Soto", "viviana.soto@example.com"),
        new("Samuel Arango", "samuel.arango@example.com"),
        new("Elena Buitrago", "elena.buitrago@example.com"),
        new("Ivan Daza", "ivan.daza@example.com"),
        new("Pilar Nieto", "pilar.nieto@example.com"),
        new("German Leal", "german.leal@example.com"),
        new("Claudia Rincon", "claudia.rincon@example.com"),
        new("Brayan Maldonado", "brayan.maldonado@example.com"),
        new("Angela Barrios", "angela.barrios@example.com"),
        new("Jose Luis Ortega", "jose.ortega@example.com"),
        new("Silvia Pena", "silvia.pena@example.com"),
        new("Martin Avila", "martin.avila@example.com"),
        new("Paola Trujillo", "paola.trujillo@example.com"),
        new("Camilo Becerra", "camilo.becerra@example.com"),
        new("Daniela Cardenas", "daniela.cardenas@example.com"),
        new("Fernando Ibarra", "fernando.ibarra@example.com"),
        new("Lina Marcela Rueda", "lina.rueda@example.com"),
        new("Victor Hugo Molina", "victor.molina@example.com"),
        new("Patricia Saavedra", "patricia.saavedra@example.com"),
        new("Juanita Escobar", "juanita.escobar@example.com"),
        new("Roberto Sarmiento", "roberto.sarmiento@example.com"),
        new("Martha Lucía Vega", "martha.vega@example.com"),
        new("Nelson Cuellar", "nelson.cuellar@example.com")
    ];

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
        else if (await db.Events.AnyAsync(ct))
        {
            logger.LogInformation("Platform seed data already exists; skipping seed.");
            return;
        }

        var venues = await db.Venues
            .Where(v => v.Id == 1 || v.Id == 2 || v.Id == 3)
            .ToDictionaryAsync(v => v.Id, ct);

        if (venues.Count != 3)
            throw new InvalidOperationException("Platform seed data requires venues with IDs 1, 2, and 3.");

        var nowBogota = ColombiaTime.NowInColombia(clock);

        var events = new[]
        {
            CreateEvent("Midudev en Bogota: Frontend que Escala", EventType.Conferencia, 1, 160, 120m, PastSchedule(nowBogota, 24, 18), "Charla tecnica sobre arquitectura frontend, rendimiento y buenas practicas para equipos web."),
            CreateEvent("Taller de Ciberseguridad para Pymes", EventType.Taller, 1, 120, 85m, PastSchedule(nowBogota, 18, 17), "Sesion practica para proteger correos, accesos, datos de clientes y operaciones digitales."),
            CreateEvent("Diamante Eléctrico - Noche Capital", EventType.Concierto, 1, 180, 180m, CurrentSchedule(nowBogota), "Concierto en formato electrico con repertorio de rock colombiano y artistas invitados."),
            CreateEvent("Arquitectura de Software para Equipos Fintech", EventType.Conferencia, 1, 150, 120m, FutureSchedule(nowBogota, 1, 18), "Encuentro sobre modularidad, deuda tecnica, observabilidad y decisiones de arquitectura."),
            CreateEvent("Marketing Digital para Marcas Culturales", EventType.Taller, 1, 110, 65m, FutureSchedule(nowBogota, 8, 18), "Workshop para planear campanas, medir audiencias y vender boleteria cultural con datos."),
            CreateEvent("Enjambre - Segunda Fecha", EventType.Concierto, 1, 180, 220m, FutureSchedule(nowBogota, 16, 20), "Segunda presentacion de Enjambre en Colombia con produccion completa para publico general."),

            CreateEvent("Turismo Regenerativo en el Eje Cafetero", EventType.Conferencia, 2, 45, 65m, PastSchedule(nowBogota, 22, 18), "Conversatorio sobre experiencias turisticas responsables, comunidades locales y sostenibilidad."),
            CreateEvent("Autodefensa Urbana para Mujeres", EventType.Taller, 2, 42, 45m, PastSchedule(nowBogota, 15, 16), "Taller introductorio de prevencion, lectura del entorno y tecnicas basicas de defensa personal."),
            CreateEvent("Enjambre - Primera Fecha", EventType.Concierto, 2, 45, 180m, CurrentSchedule(nowBogota), "Concierto intimo de Enjambre con cupo limitado y experiencia cercana al escenario."),
            CreateEvent("Producto Digital para Startups Colombianas", EventType.Conferencia, 2, 45, 85m, FutureSchedule(nowBogota, 1, 17), "Charla aplicada sobre descubrimiento, metricas, estrategia de producto y crecimiento temprano."),
            CreateEvent("Taller de Convivencia y Buen Trato Comunitario", EventType.Taller, 2, 40, 45m, FutureSchedule(nowBogota, 10, 17), "Espacio participativo para fortalecer comunicacion, mediacion y acuerdos de convivencia."),
            CreateEvent("Cuarteto de Nos - Historias Improbables", EventType.Concierto, 2, 45, 220m, FutureSchedule(nowBogota, 18, 20), "Presentacion especial de Cuarteto de Nos en formato de auditorio para seguidores de la banda."),

            CreateEvent("Gustavo Cerati Sinfonico", EventType.Concierto, 3, 420, 220m, PastSchedule(nowBogota, 28, 20), "Homenaje sinfonico al repertorio de Gustavo Cerati con banda invitada y visuales inmersivos."),
            CreateEvent("Colombia Sostenible: Empresas y Territorio", EventType.Conferencia, 3, 300, 120m, PastSchedule(nowBogota, 20, 18), "Foro empresarial sobre energia, ciudades sostenibles, biodiversidad y desarrollo regional."),
            CreateEvent("Bootcamp de Datos para Decisiones Publicas", EventType.Taller, 3, 240, 85m, CurrentSchedule(nowBogota), "Entrenamiento practico en analisis de datos, tableros e indicadores para equipos publicos."),
            CreateEvent("Conciencia en Ciberseguridad para Familias", EventType.Conferencia, 3, 260, 65m, FutureSchedule(nowBogota, 1, 19), "Charla abierta sobre fraudes digitales, privacidad, contrasenas y cuidado de menores en linea."),
            CreateEvent("Taller de Emprendimiento Creativo", EventType.Taller, 3, 220, 85m, FutureSchedule(nowBogota, 12, 18), "Laboratorio para validar propuestas culturales, calcular costos y preparar lanzamientos."),
            CreateEvent("Enjambre - Tercera Fecha", EventType.Concierto, 3, 430, 220m, FutureSchedule(nowBogota, 21, 20), "Tercera fecha de Enjambre con montaje de gran formato y zona gastronomica colombiana."),
        };

        foreach (var @event in events)
        {
            var venueCapacity = venues[@event.VenueId].Capacity;
            if (@event.MaxCapacity > venueCapacity)
                throw new InvalidOperationException($"Event '{@event.Title}' exceeds venue capacity.");
        }

        await db.Events.AddRangeAsync(events, ct);

        var codeSequence = 100001;
        var buyerIndex = 0;
        var reservations = new List<Reservation>();

        AddPastStory(reservations, events[0], ref buyerIndex, ref codeSequence, 10, 8, 6, 5);
        AddPastStory(reservations, events[1], ref buyerIndex, ref codeSequence, 14, 11, 4, 3);
        AddCurrentStory(reservations, events[2], nowBogota, ref buyerIndex, ref codeSequence, 10, 10, 7, 5);
        AddNearFutureStory(reservations, events[3], nowBogota, ref buyerIndex, ref codeSequence, 10, 10, 6, 4);
        AddFutureStory(reservations, events[4], nowBogota, ref buyerIndex, ref codeSequence, 18, 14, 8, 4);
        AddFutureStory(reservations, events[5], nowBogota, ref buyerIndex, ref codeSequence, 10, 8, 10, 6);

        AddPastStory(reservations, events[6], ref buyerIndex, ref codeSequence, 8, 7, 3, 2);
        AddPastStory(reservations, events[7], ref buyerIndex, ref codeSequence, 6, 5, 2, 2);
        AddCurrentStory(reservations, events[8], nowBogota, ref buyerIndex, ref codeSequence, 10, 5, 3, 2);
        AddNearFutureStory(reservations, events[9], nowBogota, ref buyerIndex, ref codeSequence, 9, 6, 3, 2);
        AddFutureStory(reservations, events[10], nowBogota, ref buyerIndex, ref codeSequence, 7, 6, 4, 2);
        AddFutureStory(reservations, events[11], nowBogota, ref buyerIndex, ref codeSequence, 10, 8, 4, 2);

        AddPastStory(reservations, events[12], ref buyerIndex, ref codeSequence, 10, 10, 10, 10);
        AddPastStory(reservations, events[13], ref buyerIndex, ref codeSequence, 10, 10, 10, 10);
        AddCurrentStory(reservations, events[14], nowBogota, ref buyerIndex, ref codeSequence, 55, 35, 16, 10);
        AddNearFutureStory(reservations, events[15], nowBogota, ref buyerIndex, ref codeSequence, 10, 10, 10, 8);
        AddFutureStory(reservations, events[16], nowBogota, ref buyerIndex, ref codeSequence, 36, 30, 18, 8);
        AddFutureStory(reservations, events[17], nowBogota, ref buyerIndex, ref codeSequence, 10, 10, 10, 10);

        await db.Reservations.AddRangeAsync(reservations, ct);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Seeded Colombian platform data with {EventCount} events and {ReservationCount} reservations.",
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
        new(title, type, venueId, maxCapacity, new Money(price), schedule, description);

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
        ref int buyerIndex,
        ref int codeSequence,
        int firstConfirmedTickets,
        int secondConfirmedTickets,
        int lostTickets,
        int canceledTickets)
    {
        reservations.Add(Confirmed(@event, NextBuyer(ref buyerIndex), firstConfirmedTickets, @event.Schedule.StartsAt.AddDays(-18), ref codeSequence));
        reservations.Add(Confirmed(@event, NextBuyer(ref buyerIndex), secondConfirmedTickets, @event.Schedule.StartsAt.AddDays(-14), ref codeSequence));
        reservations.Add(Lost(@event, NextBuyer(ref buyerIndex), lostTickets, @event.Schedule.StartsAt.AddDays(-10), @event.Schedule.StartsAt.AddHours(-6), ref codeSequence));
        reservations.Add(CanceledConfirmed(@event, NextBuyer(ref buyerIndex), canceledTickets, @event.Schedule.StartsAt.AddDays(-9), @event.Schedule.StartsAt.AddDays(-4), ref codeSequence));
    }

    private static void AddCurrentStory(
        List<Reservation> reservations,
        Event @event,
        DateTimeOffset nowBogota,
        ref int buyerIndex,
        ref int codeSequence,
        int confirmedTickets,
        int pendingTickets,
        int lostTickets,
        int canceledTickets)
    {
        reservations.Add(Confirmed(@event, NextBuyer(ref buyerIndex), confirmedTickets, nowBogota.AddDays(-4), ref codeSequence));
        reservations.Add(Pending(@event, NextBuyer(ref buyerIndex), pendingTickets, nowBogota.AddMinutes(-5)));
        reservations.Add(Lost(@event, NextBuyer(ref buyerIndex), lostTickets, nowBogota.AddDays(-2), @event.Schedule.StartsAt.AddHours(-8), ref codeSequence));
        reservations.Add(CanceledConfirmed(@event, NextBuyer(ref buyerIndex), canceledTickets, nowBogota.AddDays(-3), nowBogota.AddDays(-2), ref codeSequence));
    }

    private static void AddNearFutureStory(
        List<Reservation> reservations,
        Event @event,
        DateTimeOffset nowBogota,
        ref int buyerIndex,
        ref int codeSequence,
        int confirmedTickets,
        int pendingTickets,
        int lostTickets,
        int canceledTickets)
    {
        reservations.Add(Confirmed(@event, NextBuyer(ref buyerIndex), confirmedTickets, nowBogota.AddDays(-3), ref codeSequence));
        reservations.Add(Pending(@event, NextBuyer(ref buyerIndex), pendingTickets, nowBogota.AddMinutes(-4)));
        reservations.Add(Lost(@event, NextBuyer(ref buyerIndex), lostTickets, nowBogota.AddDays(-2), nowBogota.AddHours(-2), ref codeSequence));
        reservations.Add(CanceledConfirmed(@event, NextBuyer(ref buyerIndex), canceledTickets, nowBogota.AddDays(-4), nowBogota.AddDays(-3), ref codeSequence));
    }

    private static void AddFutureStory(
        List<Reservation> reservations,
        Event @event,
        DateTimeOffset nowBogota,
        ref int buyerIndex,
        ref int codeSequence,
        int firstConfirmedTickets,
        int secondConfirmedTickets,
        int pendingTickets,
        int canceledTickets)
    {
        reservations.Add(Confirmed(@event, NextBuyer(ref buyerIndex), firstConfirmedTickets, nowBogota.AddDays(-5), ref codeSequence));
        reservations.Add(Confirmed(@event, NextBuyer(ref buyerIndex), secondConfirmedTickets, nowBogota.AddDays(-3), ref codeSequence));
        reservations.Add(Pending(@event, NextBuyer(ref buyerIndex), pendingTickets, nowBogota.AddMinutes(-4)));
        reservations.Add(CanceledConfirmed(@event, NextBuyer(ref buyerIndex), canceledTickets, nowBogota.AddDays(-4), nowBogota.AddDays(-2), ref codeSequence));
    }

    private static Reservation Pending(Event @event, SeedBuyer buyer, int quantity, DateTimeOffset createdAt) =>
        new(@event.Id, new Buyer(buyer.Name, buyer.Email), quantity, createdAt);

    private static Reservation Confirmed(
        Event @event,
        SeedBuyer buyer,
        int quantity,
        DateTimeOffset createdAt,
        ref int codeSequence)
    {
        var reservation = Pending(@event, buyer, quantity, createdAt);
        reservation.Confirm(NextCode(ref codeSequence), createdAt.AddMinutes(5));
        return reservation;
    }

    private static Reservation Lost(
        Event @event,
        SeedBuyer buyer,
        int quantity,
        DateTimeOffset createdAt,
        DateTimeOffset canceledAt,
        ref int codeSequence)
    {
        var reservation = Confirmed(@event, buyer, quantity, createdAt, ref codeSequence);
        reservation.CancelConfirmed(releaseSeats: false, canceledAt);
        return reservation;
    }

    private static Reservation CanceledConfirmed(
        Event @event,
        SeedBuyer buyer,
        int quantity,
        DateTimeOffset createdAt,
        DateTimeOffset canceledAt,
        ref int codeSequence)
    {
        var reservation = Confirmed(@event, buyer, quantity, createdAt, ref codeSequence);
        reservation.CancelConfirmed(releaseSeats: true, canceledAt);
        return reservation;
    }

    private static SeedBuyer NextBuyer(ref int buyerIndex) => Buyers[buyerIndex++ % Buyers.Length];

    private static string NextCode(ref int codeSequence) => $"EV-{codeSequence++:D6}";

    private sealed record SeedBuyer(string Name, string Email);
}
