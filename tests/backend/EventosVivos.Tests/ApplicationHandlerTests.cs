using EventosVivos.Application;
using EventosVivos.Application.DTOs;
using EventosVivos.Application.Handlers;
using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Enums;
using EventosVivos.Domain.Policies;
using EventosVivos.Domain.Repositories;
using EventosVivos.Domain.Services;
using EventosVivos.Domain.ValueObjects;

namespace EventosVivos.Tests;

/// <summary>
/// In-memory event repository for handler testing.
/// </summary>
public class InMemoryEventRepository : IEventRepository
{
    private readonly List<Event> _events = new();

    public Task<TResult> ExecuteInSerializableTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken ct = default) =>
        operation(ct);

    public Task<Event?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(_events.FirstOrDefault(e => e.Id == id));

    public Task<IReadOnlyList<Event>> GetByVenueIdAsync(int venueId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Event>>(
            _events.Where(e => e.VenueId == venueId).ToList());

    public Task<IReadOnlyList<Event>> GetFilteredAsync(
        EventType? type = null, int? venueId = null,
        DateTimeOffset? startsAtFrom = null, DateTimeOffset? startsAtTo = null,
        bool? isCanceled = null, string? titleSearch = null,
        CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Event>>(
            _events.Where(e =>
                (!type.HasValue || e.Type == type) &&
                (!venueId.HasValue || e.VenueId == venueId) &&
                (!startsAtFrom.HasValue || e.Schedule.StartsAt >= startsAtFrom) &&
                (!startsAtTo.HasValue || e.Schedule.StartsAt <= startsAtTo) &&
                (!isCanceled.HasValue || e.IsCanceled == isCanceled) &&
                (string.IsNullOrWhiteSpace(titleSearch) ||
                 e.Title.Contains(titleSearch, StringComparison.OrdinalIgnoreCase))
            ).ToList());

    public Task AddAsync(Event @event, CancellationToken ct = default)
    {
        _events.Add(@event);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Event @event, CancellationToken ct = default)
    {
        var idx = _events.FindIndex(e => e.Id == @event.Id);
        if (idx >= 0) _events[idx] = @event;
        return Task.CompletedTask;
    }
}

/// <summary>
/// In-memory reservation repository for handler testing.
/// </summary>
public class InMemoryReservationRepository : IReservationRepository
{
    private readonly List<Reservation> _reservations = new();

    public Task<Reservation?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(_reservations.FirstOrDefault(r => r.Id == id));

    public Task<IReadOnlyList<Reservation>> GetByEventIdAsync(Guid eventId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Reservation>>(
            _reservations.Where(r => r.EventId == eventId).ToList());

    public Task<IReadOnlyList<Reservation>> GetFilteredAsync(
        Guid? eventId = null, string? status = null, string? buyerEmail = null,
        CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Reservation>>(
            _reservations.Where(r =>
                (!eventId.HasValue || r.EventId == eventId) &&
                (string.IsNullOrWhiteSpace(status) ||
                 r.Status.ToString().Equals(status, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrWhiteSpace(buyerEmail) ||
                 r.Buyer.Email.Contains(buyerEmail, StringComparison.OrdinalIgnoreCase))
            ).ToList());

    public Task<bool> CodeExistsAsync(string code, CancellationToken ct = default) =>
        Task.FromResult(_reservations.Any(r => r.Code == code));

    public Task AddAsync(Reservation reservation, CancellationToken ct = default)
    {
        _reservations.Add(reservation);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Reservation reservation, CancellationToken ct = default)
    {
        var idx = _reservations.FindIndex(r => r.Id == reservation.Id);
        if (idx >= 0) _reservations[idx] = reservation;
        return Task.CompletedTask;
    }
}

/// <summary>
/// In-memory venue repository seeded with the 3 PDF venues.
/// </summary>
public class InMemoryVenueRepository : IVenueRepository
{
    private readonly List<Venue> _venues = new()
    {
        new(1, "Auditorio Central", 200, "Bogotá"),
        new(2, "Sala Norte", 50, "Bogotá"),
        new(3, "Arena Sur", 500, "Medellín"),
    };

    public Task<Venue?> GetByIdAsync(int id, CancellationToken ct = default) =>
        Task.FromResult(_venues.FirstOrDefault(v => v.Id == id));

    public Task<IReadOnlyList<Venue>> GetAllAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Venue>>(_venues.ToList());
}

/// <summary>
/// Helper to set up a complete handler test environment with fresh repos and a seed event.
/// </summary>
public static class HandlerTestSetup
{
    public static readonly DateTimeOffset FutureDate =
        new(2026, 8, 1, 14, 0, 0, TimeSpan.Zero);
    public static readonly DateTimeOffset FutureEnd =
        new(2026, 8, 1, 16, 0, 0, TimeSpan.Zero);

    public static FakeClock DefaultClock =>
        new(new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero));

    public static (InMemoryEventRepository events, InMemoryReservationRepository reservations,
        InMemoryVenueRepository venues, FakeClock clock, Guid eventId) CreateWithSeed()
    {
        var events = new InMemoryEventRepository();
        var reservations = new InMemoryReservationRepository();
        var venues = new InMemoryVenueRepository();
        var clock = DefaultClock;

        var handler = new CreateEventHandler(events, venues, clock);
        var result = handler.HandleAsync(
            new CreateEventRequest("Seed Event", "Seed event description", 3, 100,
                FutureDate, FutureEnd, 50, "conferencia")).GetAwaiter().GetResult();

        return (events, reservations, venues, clock, result.Data);
    }
}

/// <summary>
/// Tests that each use isolated fresh repos (no state leak).
/// </summary>
public class ApplicationHandlerTests
{
    // ─── CreateEvent Tests ───────────────────────────────────────────────

    [Fact]
    public async Task CreateEvent_WithValidData_ReturnsEventId()
    {
        var (events, _, venues, clock, _) = HandlerTestSetup.CreateWithSeed();
        var handler = new CreateEventHandler(events, venues, clock);
        var request = new CreateEventRequest(
            "Angular Summit", "A conference about Angular",
            2, 40, HandlerTestSetup.FutureDate, HandlerTestSetup.FutureEnd,
            80, "conferencia");

        var result = await handler.HandleAsync(request);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Data);
    }

    [Theory]
    [InlineData("Abcd", "Valid event description", "title")]
    [InlineData("This title is intentionally longer than one hundred characters to validate the technical test title limit rule", "Valid event description", "title")]
    [InlineData("Valid Title", "", "description")]
    [InlineData("Valid Title", "Too short", "description")]
    public async Task CreateEvent_WithInvalidTitleOrDescription_ReturnsFailure(
        string title, string description, string expectedError)
    {
        var (events, _, venues, clock, _) = HandlerTestSetup.CreateWithSeed();
        var handler = new CreateEventHandler(events, venues, clock);
        var request = new CreateEventRequest(
            title, description, 2, 40,
            HandlerTestSetup.FutureDate.AddDays(10),
            HandlerTestSetup.FutureEnd.AddDays(10),
            80, "conferencia");

        var result = await handler.HandleAsync(request);

        Assert.True(result.IsFailure);
        Assert.Contains(expectedError, result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateEvent_WithDescriptionLongerThan500_ReturnsFailure()
    {
        var (events, _, venues, clock, _) = HandlerTestSetup.CreateWithSeed();
        var handler = new CreateEventHandler(events, venues, clock);
        var request = new CreateEventRequest(
            "Valid Title", new string('a', 501), 2, 40,
            HandlerTestSetup.FutureDate.AddDays(10),
            HandlerTestSetup.FutureEnd.AddDays(10),
            80, "conferencia");

        var result = await handler.HandleAsync(request);

        Assert.True(result.IsFailure);
        Assert.Contains("description", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateEvent_WithInvalidType_ReturnsFailure()
    {
        var (events, _, venues, clock, _) = HandlerTestSetup.CreateWithSeed();
        var handler = new CreateEventHandler(events, venues, clock);
        var request = new CreateEventRequest(
            "Bad Type", "Valid event description", 1, 100, HandlerTestSetup.FutureDate,
            HandlerTestSetup.FutureEnd, 80, "invalid_type");

        var result = await handler.HandleAsync(request);

        Assert.True(result.IsFailure);
        Assert.Contains("Invalid event type", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateEvent_WithNonexistentVenue_ReturnsFailure()
    {
        var (events, _, venues, clock, _) = HandlerTestSetup.CreateWithSeed();
        var handler = new CreateEventHandler(events, venues, clock);
        var request = new CreateEventRequest(
            "No Venue", "Valid event description", 999, 100, HandlerTestSetup.FutureDate,
            HandlerTestSetup.FutureEnd, 80, "conferencia");

        var result = await handler.HandleAsync(request);

        Assert.True(result.IsFailure);
        Assert.Contains("not found", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateEvent_WithExcessCapacity_ReturnsFailure()
    {
        var (events, _, venues, clock, _) = HandlerTestSetup.CreateWithSeed();
        var handler = new CreateEventHandler(events, venues, clock);
        var request = new CreateEventRequest(
            "Overflow", "Valid event description", 2, 51, HandlerTestSetup.FutureDate,
            HandlerTestSetup.FutureEnd, 80, "taller");

        var result = await handler.HandleAsync(request);

        Assert.True(result.IsFailure);
        Assert.Contains("cannot exceed", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateEvent_WithOverlappingSchedule_ReturnsFailure()
    {
        var (events, _, venues, clock, _) = HandlerTestSetup.CreateWithSeed();
        var handler = new CreateEventHandler(events, venues, clock);

        // Same venue (3) as seed event at overlapping time
        var overlapping = new CreateEventRequest(
            "Overlap", "Valid event description", 3, 50,
            HandlerTestSetup.FutureDate.AddHours(1),
            HandlerTestSetup.FutureEnd.AddHours(1), 80, "conferencia");

        var result = await handler.HandleAsync(overlapping);

        Assert.True(result.IsFailure);
        Assert.Contains("overlap", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    // ─── ReserveTickets Tests ───────────────────────────────────────────

    [Fact]
    public async Task ReserveTickets_WithValidData_ReturnsReservationResponse()
    {
        var (events, reservations, venues, clock, eventId) = HandlerTestSetup.CreateWithSeed();
        var handler = new ReserveTicketsHandler(events, reservations, clock);
        var request = new ReserveTicketsRequest(
            eventId, 3, "Ana Pérez", "ana@example.com");

        var result = await handler.HandleAsync(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("pendiente_pago", result.Data.Status);
        Assert.Equal(3, result.Data.Quantity);
    }

    [Fact]
    public async Task ReserveTickets_WithInvalidEmail_ReturnsFailure()
    {
        var (events, reservations, _, clock, eventId) = HandlerTestSetup.CreateWithSeed();
        var handler = new ReserveTicketsHandler(events, reservations, clock);
        var request = new ReserveTicketsRequest(
            eventId, 1, "Ana Pérez", "not-an-email");

        var result = await handler.HandleAsync(request);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task ReserveTickets_WithShortName_ReturnsFailure()
    {
        var (events, reservations, _, clock, eventId) = HandlerTestSetup.CreateWithSeed();
        var handler = new ReserveTicketsHandler(events, reservations, clock);
        var request = new ReserveTicketsRequest(
            eventId, 1, "A", "ana@example.com");

        var result = await handler.HandleAsync(request);

        Assert.True(result.IsFailure);
        Assert.Contains("2", result.Error);
    }

    [Fact]
    public async Task ReserveTickets_ForNonexistentEvent_ReturnsFailure()
    {
        var (_, reservations, _, clock, _) = HandlerTestSetup.CreateWithSeed();
        var events = new InMemoryEventRepository(); // empty repo
        var handler = new ReserveTicketsHandler(events, reservations, clock);
        var request = new ReserveTicketsRequest(
            Guid.NewGuid(), 1, "Ana Pérez", "ana@example.com");

        var result = await handler.HandleAsync(request);

        Assert.True(result.IsFailure);
        Assert.Contains("not found", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    // ─── ConfirmPayment Tests ────────────────────────────────────────────

    [Fact]
    public async Task ConfirmPayment_OnPendingReservation_ReturnsConfirmed()
    {
        var (events, reservations, _, clock, eventId) = HandlerTestSetup.CreateWithSeed();

        var reserveHandler = new ReserveTicketsHandler(events, reservations, clock);
        var reserveResult = await reserveHandler.HandleAsync(
            new ReserveTicketsRequest(eventId, 2, "Juan Lopez", "juan@test.com"));
        Assert.True(reserveResult.IsSuccess);

        var confirmHandler = new ConfirmPaymentHandler(reservations, clock);
        var confirmResult = await confirmHandler.HandleAsync(
            new ConfirmPaymentRequest(reserveResult.Data!.Id));

        Assert.True(confirmResult.IsSuccess);
        Assert.Equal("confirmada", confirmResult.Data!.Status);
        Assert.NotNull(confirmResult.Data.Code);
        Assert.StartsWith("EV-", confirmResult.Data.Code);
    }

    [Fact]
    public async Task ConfirmPayment_OnAlreadyConfirmed_Fails()
    {
        var (events, reservations, _, clock, eventId) = HandlerTestSetup.CreateWithSeed();

        var reserveHandler = new ReserveTicketsHandler(events, reservations, clock);
        var reserveResult = await reserveHandler.HandleAsync(
            new ReserveTicketsRequest(eventId, 1, "Test", "test@test.com"));
        Assert.True(reserveResult.IsSuccess);

        var confirmHandler = new ConfirmPaymentHandler(reservations, clock);
        await confirmHandler.HandleAsync(new ConfirmPaymentRequest(reserveResult.Data!.Id));

        var secondConfirm = await confirmHandler.HandleAsync(
            new ConfirmPaymentRequest(reserveResult.Data!.Id));

        Assert.True(secondConfirm.IsFailure);
    }

    [Fact]
    public async Task ConfirmPayment_OnNonexistentReservation_Fails()
    {
        var (_, reservations, _, clock, _) = HandlerTestSetup.CreateWithSeed();
        var confirmHandler = new ConfirmPaymentHandler(reservations, clock);
        var result = await confirmHandler.HandleAsync(
            new ConfirmPaymentRequest(Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.Contains("not found", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ConfirmPayment_OnExpiredPendingReservation_Fails()
    {
        var (events, reservations, _, clock, eventId) = HandlerTestSetup.CreateWithSeed();

        var reserveHandler = new ReserveTicketsHandler(events, reservations, clock);
        var reserveResult = await reserveHandler.HandleAsync(
            new ReserveTicketsRequest(eventId, 2, "Expired Buyer", "expired@test.com"));
        Assert.True(reserveResult.IsSuccess);

        var expiredClock = new FakeClock(clock.UtcNow.AddMinutes(16));
        var confirmHandler = new ConfirmPaymentHandler(reservations, expiredClock);
        var result = await confirmHandler.HandleAsync(
            new ConfirmPaymentRequest(reserveResult.Data!.Id));

        Assert.True(result.IsFailure);
        Assert.Contains("expired", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    // ─── CancelReservation Tests ─────────────────────────────────────────

    [Fact]
    public async Task CancelReservation_OnPending_ReturnsCancelada()
    {
        var (events, reservations, _, clock, eventId) = HandlerTestSetup.CreateWithSeed();

        var reserveHandler = new ReserveTicketsHandler(events, reservations, clock);
        var reserveResult = await reserveHandler.HandleAsync(
            new ReserveTicketsRequest(eventId, 5, "Cancel Test", "cancel@test.com"));
        Assert.True(reserveResult.IsSuccess);

        var cancelHandler = new CancelReservationHandler(reservations, events, clock);
        var cancelResult = await cancelHandler.HandleAsync(
            new CancelReservationRequest(reserveResult.Data!.Id));

        Assert.True(cancelResult.IsSuccess);
        Assert.Equal("cancelada", cancelResult.Data!.Status);
    }

    [Fact]
    public async Task CancelReservation_OnConfirmedEarly_ReturnsCancelada()
    {
        var (events, reservations, _, clock, eventId) = HandlerTestSetup.CreateWithSeed();

        var reserveHandler = new ReserveTicketsHandler(events, reservations, clock);
        var reserveResult = await reserveHandler.HandleAsync(
            new ReserveTicketsRequest(eventId, 3, "Early", "early@test.com"));
        Assert.True(reserveResult.IsSuccess);

        var confirmHandler = new ConfirmPaymentHandler(reservations, clock);
        await confirmHandler.HandleAsync(new ConfirmPaymentRequest(reserveResult.Data!.Id));

        // Event is in August, we're in June — well over 48h before
        var cancelHandler = new CancelReservationHandler(reservations, events, clock);
        var cancelResult = await cancelHandler.HandleAsync(
            new CancelReservationRequest(reserveResult.Data!.Id));

        Assert.True(cancelResult.IsSuccess);
        Assert.Equal("cancelada", cancelResult.Data!.Status);
    }

    [Fact]
    public async Task CancelReservation_OnAlreadyCanceled_ReturnsClearFailure()
    {
        var (events, reservations, _, clock, eventId) = HandlerTestSetup.CreateWithSeed();

        var reserveHandler = new ReserveTicketsHandler(events, reservations, clock);
        var reserveResult = await reserveHandler.HandleAsync(
            new ReserveTicketsRequest(eventId, 1, "Canceled", "canceled@test.com"));
        Assert.True(reserveResult.IsSuccess);

        var cancelHandler = new CancelReservationHandler(reservations, events, clock);
        await cancelHandler.HandleAsync(new CancelReservationRequest(reserveResult.Data!.Id));

        var secondCancel = await cancelHandler.HandleAsync(
            new CancelReservationRequest(reserveResult.Data!.Id));

        Assert.True(secondCancel.IsFailure);
        Assert.Contains("already canceled", secondCancel.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CancelReservation_OnLost_ReturnsClearFailure()
    {
        var (events, reservations, _, clock, eventId) = HandlerTestSetup.CreateWithSeed();

        var reserveHandler = new ReserveTicketsHandler(events, reservations, clock);
        var reserveResult = await reserveHandler.HandleAsync(
            new ReserveTicketsRequest(eventId, 1, "Lost", "lost@test.com"));
        Assert.True(reserveResult.IsSuccess);

        var confirmHandler = new ConfirmPaymentHandler(reservations, clock);
        await confirmHandler.HandleAsync(new ConfirmPaymentRequest(reserveResult.Data!.Id));

        var lateClock = new FakeClock(HandlerTestSetup.FutureDate.AddHours(-47));
        var cancelHandler = new CancelReservationHandler(reservations, events, lateClock);
        await cancelHandler.HandleAsync(new CancelReservationRequest(reserveResult.Data!.Id));

        var secondCancel = await cancelHandler.HandleAsync(
            new CancelReservationRequest(reserveResult.Data!.Id));

        Assert.True(secondCancel.IsFailure);
        Assert.Contains("already lost", secondCancel.Error, StringComparison.OrdinalIgnoreCase);
    }

    // ─── CancelEvent Tests ───────────────────────────────────────────────

    [Fact]
    public async Task CancelEvent_OnActiveEvent_MarksCanceled()
    {
        var (events, _, venues, clock, _) = HandlerTestSetup.CreateWithSeed();

        var handler = new CreateEventHandler(events, venues, clock);
        // Use venue 2 — no overlap with seed event in venue 3
        var createResult = await handler.HandleAsync(
            new CreateEventRequest("To Cancel", "Valid event description", 2, 30,
                HandlerTestSetup.FutureDate, HandlerTestSetup.FutureEnd,
                50, "conferencia"));
        Assert.True(createResult.IsSuccess);

        var cancelHandler = new CancelEventHandler(events, clock);
        var result = await cancelHandler.HandleAsync(
            new CancelEventRequest(createResult.Data!));

        Assert.True(result.IsSuccess);

        var ev = await events.GetByIdAsync(createResult.Data!);
        Assert.NotNull(ev);
        Assert.True(ev.IsCanceled);
    }

    [Fact]
    public async Task CancelEvent_OnAlreadyCanceled_Fails()
    {
        var (events, _, venues, clock, _) = HandlerTestSetup.CreateWithSeed();

        var handler = new CreateEventHandler(events, venues, clock);
        // Use venue 2 — no overlap
        var createResult = await handler.HandleAsync(
            new CreateEventRequest("Already Canceled", "Valid event description", 2, 30,
                HandlerTestSetup.FutureDate, HandlerTestSetup.FutureEnd,
                50, "concierto"));
        Assert.True(createResult.IsSuccess);

        var cancelHandler = new CancelEventHandler(events, clock);
        await cancelHandler.HandleAsync(new CancelEventRequest(createResult.Data!));

        var secondCancel = await cancelHandler.HandleAsync(
            new CancelEventRequest(createResult.Data!));

        Assert.True(secondCancel.IsFailure);
        Assert.Contains("already canceled",
            secondCancel.Error, StringComparison.OrdinalIgnoreCase);
    }

    // ─── ListEvents Tests ────────────────────────────────────────────────

    [Fact]
    public async Task ListEvents_WithTypeFilter_ReturnsMatching()
    {
        var (events, _, venues, clock, _) = HandlerTestSetup.CreateWithSeed();

        var handler = new CreateEventHandler(events, venues, clock);
        await handler.HandleAsync(new CreateEventRequest(
            "Workshop", "Workshop description", 2, 30,
            HandlerTestSetup.FutureDate, HandlerTestSetup.FutureEnd, 30, "taller"));

        var listHandler = new ListEventsHandler(events, clock);
        var result = await listHandler.HandleAsync(new ListEventsQuery(Type: "taller"));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Contains(result.Data.Items, e => e.Title == "Workshop");
    }

    [Fact]
    public async Task ListEvents_WithTitleSearch_ReturnsMatching()
    {
        var (events, _, venues, clock, _) = HandlerTestSetup.CreateWithSeed();

        var handler = new CreateEventHandler(events, venues, clock);
        await handler.HandleAsync(new CreateEventRequest(
            "Festival Bogotá", "Festival description", 2, 30,
            HandlerTestSetup.FutureDate, HandlerTestSetup.FutureEnd,
            50, "conferencia"));

        var listHandler = new ListEventsHandler(events, clock);
        var result = await listHandler.HandleAsync(
            new ListEventsQuery(TitleSearch: "festival"));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Contains(result.Data.Items, e => e.Title.Contains("Festival"));
    }

    [Fact]
    public async Task ListEvents_WithCompletadoStatusFilter_ReturnsCompletedEvents()
    {
        var (events, _, venues, createClock, _) = HandlerTestSetup.CreateWithSeed();

        // Create a past event via handler (using a clock set before its dates)
        var earlyClock = new FakeClock(new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero));
        var handler = new CreateEventHandler(events, venues, earlyClock);
        var pastStart = new DateTimeOffset(2026, 5, 15, 14, 0, 0, TimeSpan.Zero);
        var pastEnd = new DateTimeOffset(2026, 5, 15, 16, 0, 0, TimeSpan.Zero);
        var createResult = await handler.HandleAsync(
            new CreateEventRequest("Completed Past Event", "Valid event description", 2, 30,
                pastStart, pastEnd, 50, "taller"));
        Assert.True(createResult.IsSuccess);

        // Query with a clock AFTER the event's end time
        var laterClock = new FakeClock(new DateTimeOffset(2026, 6, 1, 14, 0, 0, TimeSpan.Zero));
        var listHandler = new ListEventsHandler(events, laterClock);
        var result = await listHandler.HandleAsync(
            new ListEventsQuery(Status: "completado"));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.Items);
        Assert.Contains(result.Data.Items, e => e.Title == "Completed Past Event");
        Assert.All(result.Data.Items, e => Assert.Equal("completado", e.Status));
    }

    [Fact]
    public async Task ListEvents_WithActivoStatusFilter_ReturnsActiveEvents()
    {
        var (events, _, venues, clock, _) = HandlerTestSetup.CreateWithSeed();

        // Seed event is in the future (August), clock is June — it should be Activo
        var listHandler = new ListEventsHandler(events, clock);
        var result = await listHandler.HandleAsync(
            new ListEventsQuery(Status: "activo"));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.Items);
        Assert.All(result.Data.Items, e => Assert.Equal("activo", e.Status));
    }

    [Fact]
    public async Task GetEvent_ReturnsLowercaseApiEnumValues()
    {
        var (events, _, _, clock, eventId) = HandlerTestSetup.CreateWithSeed();
        var handler = new GetEventHandler(events, clock);

        var result = await handler.HandleAsync(new GetEventQuery(eventId));

        Assert.True(result.IsSuccess);
        Assert.Equal("conferencia", result.Data!.Type);
        Assert.Equal("activo", result.Data.Status);
    }

    [Fact]
    public async Task ListEvents_WithPagination_ReturnsPagedMetadata()
    {
        var (events, _, venues, clock, _) = HandlerTestSetup.CreateWithSeed();

        // Create 3 more events (total 4)
        var handler = new CreateEventHandler(events, venues, clock);
        for (var i = 0; i < 3; i++)
        {
            var start = HandlerTestSetup.FutureDate.AddDays((i + 1) * 7);
            var end = start.AddHours(2);
            await handler.HandleAsync(new CreateEventRequest(
                $"Pagination Event {i + 1}", "Pagination event description", 3, 50,
                start, end, 30, "taller"));
        }

        var listHandler = new ListEventsHandler(events, clock);

        // Page 1 with pageSize 2
        var page1 = await listHandler.HandleAsync(new ListEventsQuery(PageSize: 2, PageNumber: 1));
        Assert.True(page1.IsSuccess);
        Assert.NotNull(page1.Data);
        Assert.Equal(2, page1.Data.Items.Count);
        Assert.Equal(4, page1.Data.TotalCount);
        Assert.Equal(2, page1.Data.TotalPages);
        Assert.False(page1.Data.HasPreviousPage);
        Assert.True(page1.Data.HasNextPage);

        // Page 2 with pageSize 2
        var page2 = await listHandler.HandleAsync(new ListEventsQuery(PageSize: 2, PageNumber: 2));
        Assert.True(page2.IsSuccess);
        Assert.Equal(2, page2.Data!.Items.Count);
        Assert.True(page2.Data.HasPreviousPage);
        Assert.False(page2.Data.HasNextPage);

        // Verify stable ordering by StartsAt: seed event ("Seed Event") comes first, then pagination events
        Assert.Equal("Seed Event", page1.Data.Items[0].Title);
        Assert.Equal("Pagination Event 1", page1.Data.Items[1].Title);
    }

    [Fact]
    public async Task ListEvents_WithInvalidPageNumber_ReturnsFailure()
    {
        var (events, _, _, clock, _) = HandlerTestSetup.CreateWithSeed();
        var handler = new ListEventsHandler(events, clock);

        var result = await handler.HandleAsync(new ListEventsQuery(PageNumber: 0));
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task ListEvents_WithInvalidPageSize_ReturnsFailure()
    {
        var (events, _, _, clock, _) = HandlerTestSetup.CreateWithSeed();
        var handler = new ListEventsHandler(events, clock);

        var result = await handler.HandleAsync(new ListEventsQuery(PageSize: 0));
        Assert.True(result.IsFailure);

        result = await handler.HandleAsync(new ListEventsQuery(PageSize: 51));
        Assert.True(result.IsFailure);
    }

    // ─── OccupancyReport Tests ───────────────────────────────────────────

    [Fact]
    public async Task OccupancyReport_WithMixedReservations_ReturnsCorrectMetrics()
    {
        var events = new InMemoryEventRepository();
        var reservations = new InMemoryReservationRepository();
        var venues = new InMemoryVenueRepository();
        var clock = HandlerTestSetup.DefaultClock;

        // Create event
        var createHandler = new CreateEventHandler(events, venues, clock);
        var createResult = await createHandler.HandleAsync(
            new CreateEventRequest("Report Event", "Valid event description", 1, 100,
                HandlerTestSetup.FutureDate, HandlerTestSetup.FutureEnd,
                50, "conferencia"));
        Assert.True(createResult.IsSuccess);
        var eventId = createResult.Data!;

        // 2 confirmed reservations: 2 + 3 = 5 tickets
        var reserveHandler = new ReserveTicketsHandler(events, reservations, clock);
        var confirmHandler = new ConfirmPaymentHandler(reservations, clock);

        var r1 = await reserveHandler.HandleAsync(
            new ReserveTicketsRequest(eventId, 2, "Alice", "alice@test.com"));
        Assert.True(r1.IsSuccess);
        await confirmHandler.HandleAsync(new ConfirmPaymentRequest(r1.Data!.Id));

        var r2 = await reserveHandler.HandleAsync(
            new ReserveTicketsRequest(eventId, 3, "Bob", "bob@test.com"));
        Assert.True(r2.IsSuccess);
        await confirmHandler.HandleAsync(new ConfirmPaymentRequest(r2.Data!.Id));

        // 1 lost reservation: confirm, then cancel late (< 48h before event)
        var r3 = await reserveHandler.HandleAsync(
            new ReserveTicketsRequest(eventId, 2, "Carol", "carol@test.com"));
        Assert.True(r3.IsSuccess);
        await confirmHandler.HandleAsync(new ConfirmPaymentRequest(r3.Data!.Id));

        var lateClock = new FakeClock(HandlerTestSetup.FutureDate.AddHours(-47));
        var cancelHandler = new CancelReservationHandler(reservations, events, lateClock);
        await cancelHandler.HandleAsync(new CancelReservationRequest(r3.Data!.Id));

        // Get report
        var reportHandler = new GetOccupancyReportHandler(events, reservations, clock);
        var report = await reportHandler.HandleAsync(
            new GetOccupancyReportQuery(eventId));

        Assert.True(report.IsSuccess);
        Assert.Equal(5, report.Data!.ConfirmedTickets);      // 2+3
        Assert.Equal(2, report.Data.LostTickets);            // late-canceled
        Assert.Equal(93, report.Data.AvailableTickets);      // 100-5-2
        Assert.Equal(250, report.Data.Revenue);              // 50*5
        Assert.Equal(5.0m, report.Data.OccupancyPercentage); // 5/100
    }

    [Fact]
    public async Task OccupancyReport_WhenCapacityExceeded_AvailabilityIsNeverNegative()
    {
        var events = new InMemoryEventRepository();
        var reservations = new InMemoryReservationRepository();
        var venues = new InMemoryVenueRepository();
        var clock = HandlerTestSetup.DefaultClock;

        // Create a small-capacity event
        var createHandler = new CreateEventHandler(events, venues, clock);
        var createResult = await createHandler.HandleAsync(
            new CreateEventRequest("Small Event", "Valid event description", 2, 10,
                HandlerTestSetup.FutureDate, HandlerTestSetup.FutureEnd,
                50, "conferencia"));
        Assert.True(createResult.IsSuccess);
        var eventId = createResult.Data!;

        // Fill capacity via handler: 5 + 5 = 10 confirmed
        var reserveHandler = new ReserveTicketsHandler(events, reservations, clock);
        var confirmHandler = new ConfirmPaymentHandler(reservations, clock);

        var r1 = await reserveHandler.HandleAsync(
            new ReserveTicketsRequest(eventId, 5, "Alice", "alice@test.com"));
        Assert.True(r1.IsSuccess);
        await confirmHandler.HandleAsync(new ConfirmPaymentRequest(r1.Data!.Id));

        var r2 = await reserveHandler.HandleAsync(
            new ReserveTicketsRequest(eventId, 5, "Bob", "bob@test.com"));
        Assert.True(r2.IsSuccess);
        await confirmHandler.HandleAsync(new ConfirmPaymentRequest(r2.Data!.Id));

        // Add overflow lost tickets directly (bypass handler policy) to test the Math.Max(0, …) guard
        var buyer = new Domain.ValueObjects.Buyer("Overflow", "overflow@test.com");
        var overflow = new Reservation(eventId, buyer, 3, clock.UtcNow);
        overflow.Confirm("EV-999999", clock.UtcNow);
        await reservations.AddAsync(overflow);

        var lateClock = new FakeClock(HandlerTestSetup.FutureDate.AddHours(-47));
        overflow.CancelConfirmed(false, lateClock.UtcNow);
        await reservations.UpdateAsync(overflow);

        // Report: 10 confirmed + 3 lost = 13 held > 10 capacity
        var reportHandler = new GetOccupancyReportHandler(events, reservations, clock);
        var report = await reportHandler.HandleAsync(
            new GetOccupancyReportQuery(eventId));

        Assert.True(report.IsSuccess);
        Assert.True(report.Data!.AvailableTickets >= 0,
            $"Available tickets ({report.Data.AvailableTickets}) must never be negative.");
        Assert.Equal(0, report.Data.AvailableTickets);
    }

    [Fact]
    public async Task ReservationInventorySnapshot_ComputesAvailabilityAndIgnoresExpiredPending()
    {
        var (events, reservations, _, clock, eventId) = HandlerTestSetup.CreateWithSeed();
        var @event = await events.GetByIdAsync(eventId);
        Assert.NotNull(@event);

        var reserveHandler = new ReserveTicketsHandler(events, reservations, clock);
        var confirmHandler = new ConfirmPaymentHandler(reservations, clock);

        var confirmed = await reserveHandler.HandleAsync(
            new ReserveTicketsRequest(eventId, 5, "Confirmed", "confirmed@test.com"));
        Assert.True(confirmed.IsSuccess);
        await confirmHandler.HandleAsync(new ConfirmPaymentRequest(confirmed.Data!.Id));

        var pending = await reserveHandler.HandleAsync(
            new ReserveTicketsRequest(eventId, 3, "Pending", "pending@test.com"));
        Assert.True(pending.IsSuccess);

        var expired = await reserveHandler.HandleAsync(
            new ReserveTicketsRequest(eventId, 4, "Expired", "expired-hold@test.com"));
        Assert.True(expired.IsSuccess);

        var snapshot = ReservationInventorySnapshot.From(
            @event,
            await reservations.GetByEventIdAsync(eventId),
            clock.UtcNow.AddMinutes(16),
            pending.Data!.Id);

        Assert.Equal(5, snapshot.ConfirmedTickets);
        Assert.Equal(0, snapshot.ActivePendingTickets);
        Assert.Equal(95, snapshot.AvailableTickets);
        Assert.Equal(250, snapshot.Revenue);
        Assert.Equal(5.0m, Math.Round(snapshot.OccupancyPercentage, 1));
    }

    [Fact]
    public async Task OccupancyReport_ForNonexistentEvent_ReturnsFailure()
    {
        var events = new InMemoryEventRepository();
        var reservations = new InMemoryReservationRepository();
        var clock = HandlerTestSetup.DefaultClock;
        var reportHandler = new GetOccupancyReportHandler(events, reservations, clock);

        var result = await reportHandler.HandleAsync(
            new GetOccupancyReportQuery(Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.Contains("not found", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    // ─── Reservation.CancelPending timestamp fix ──────────────────────────

    [Fact]
    public void CancelPending_WithExplicitTimestamp_DoesNotUseUtcNow()
    {
        var now = new DateTimeOffset(2026, 6, 15, 10, 30, 0, TimeSpan.Zero);
        var buyer = new Buyer("Test User", "test@example.com");
        var reservation = new Reservation(Guid.NewGuid(), buyer, 2, now);

        var cancelTime = new DateTimeOffset(2026, 6, 15, 11, 0, 0, TimeSpan.Zero);
        reservation.CancelPending(cancelTime);

        Assert.Equal(ReservationStatus.Cancelada, reservation.Status);
        Assert.Equal(cancelTime, reservation.CanceledAt);
    }

    [Fact]
    public void CancelPending_WithDifferentTimestamp_RecordsCorrectly()
    {
        var createTime = new DateTimeOffset(2026, 6, 15, 8, 0, 0, TimeSpan.Zero);
        var buyer = new Buyer("User", "user@test.com");
        var reservation = new Reservation(Guid.NewGuid(), buyer, 1, createTime);

        var cancelTime = new DateTimeOffset(2026, 6, 15, 8, 30, 0, TimeSpan.Zero);
        reservation.CancelPending(cancelTime);

        Assert.Equal(cancelTime, reservation.CanceledAt);
        Assert.NotEqual(createTime, reservation.CanceledAt);
    }

    // ─── GetEventHandler Tests ─────────────────────────────────────────────

    [Fact]
    public async Task GetEvent_WithExistingId_ReturnsEvent()
    {
        var (events, _, venues, clock, eventId) = HandlerTestSetup.CreateWithSeed();
        var handler = new GetEventHandler(events, clock);

        var result = await handler.HandleAsync(new GetEventQuery(eventId));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(eventId, result.Data.Id);
        Assert.Equal("Seed Event", result.Data.Title);
    }

    [Fact]
    public async Task GetEvent_WithNonexistentId_ReturnsFailure()
    {
        var events = new InMemoryEventRepository();
        var clock = HandlerTestSetup.DefaultClock;
        var handler = new GetEventHandler(events, clock);

        var result = await handler.HandleAsync(new GetEventQuery(Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.Contains("not found", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    // ─── UpdateEventHandler Tests ──────────────────────────────────────────

    [Fact]
    public async Task UpdateEvent_WithValidData_UpdatesSuccessfully()
    {
        var (events, _, venues, clock, eventId) = HandlerTestSetup.CreateWithSeed();
        var handler = new UpdateEventHandler(events, venues, clock);

        var result = await handler.HandleAsync(new UpdateEventRequest(
            eventId, "Updated Title", "Updated description", 1, 80,
            HandlerTestSetup.FutureDate, HandlerTestSetup.FutureEnd, 60, "taller"));

        Assert.True(result.IsSuccess);

        var updated = await events.GetByIdAsync(eventId);
        Assert.NotNull(updated);
        Assert.Equal("Updated Title", updated.Title);
    }

    [Fact]
    public async Task UpdateEvent_WithNonexistentId_ReturnsFailure()
    {
        var events = new InMemoryEventRepository();
        var venues = new InMemoryVenueRepository();
        var clock = HandlerTestSetup.DefaultClock;
        var handler = new UpdateEventHandler(events, venues, clock);

        var result = await handler.HandleAsync(new UpdateEventRequest(
            Guid.NewGuid(), "Title", null, 1, 100,
            HandlerTestSetup.FutureDate, HandlerTestSetup.FutureEnd, 50, "conferencia"));

        Assert.True(result.IsFailure);
        Assert.Contains("not found", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateEvent_WithExcessCapacity_ReturnsFailure()
    {
        var (events, _, venues, clock, eventId) = HandlerTestSetup.CreateWithSeed();
        var handler = new UpdateEventHandler(events, venues, clock);

        // venue 3 has capacity 500, so 501 should fail
        var result = await handler.HandleAsync(new UpdateEventRequest(
            eventId, "Over Capacity", "Valid event description", 3, 501,
            HandlerTestSetup.FutureDate, HandlerTestSetup.FutureEnd, 50, "conferencia"));

        Assert.True(result.IsFailure);
        Assert.Contains("cannot exceed", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    // ─── ListReservationsHandler Tests ─────────────────────────────────────

    [Fact]
    public async Task ListReservations_WithNoFilters_ReturnsAll()
    {
        var (events, reservations, _, clock, eventId) = HandlerTestSetup.CreateWithSeed();

        var reserveHandler = new ReserveTicketsHandler(events, reservations, clock);
        await reserveHandler.HandleAsync(new ReserveTicketsRequest(eventId, 2, "Alice", "alice@test.com"));
        await reserveHandler.HandleAsync(new ReserveTicketsRequest(eventId, 3, "Bob", "bob@test.com"));

        var listHandler = new ListReservationsHandler(reservations);
        var result = await listHandler.HandleAsync(new ListReservationsQuery());

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Items.Count);
    }

    [Fact]
    public async Task ListReservations_WithEventIdFilter_ReturnsFiltered()
    {
        var (events, reservations, _, clock, eventId) = HandlerTestSetup.CreateWithSeed();

        var reserveHandler = new ReserveTicketsHandler(events, reservations, clock);
        await reserveHandler.HandleAsync(new ReserveTicketsRequest(eventId, 2, "Alice", "alice@test.com"));

        var listHandler = new ListReservationsHandler(reservations);
        var result = await listHandler.HandleAsync(new ListReservationsQuery(EventId: eventId));

        Assert.True(result.IsSuccess);
        Assert.Single(result.Data!.Items);
    }

    [Fact]
    public async Task ListReservations_WithPagination_ReturnsPagedMetadata()
    {
        var (events, reservations, _, clock, eventId) = HandlerTestSetup.CreateWithSeed();

        var reserveHandler = new ReserveTicketsHandler(events, reservations, clock);
        for (var i = 0; i < 5; i++)
        {
            await reserveHandler.HandleAsync(new ReserveTicketsRequest(
                eventId, 1, $"User {i}", $"user{i}@test.com"));
        }

        var listHandler = new ListReservationsHandler(reservations);

        // Page 1 with pageSize 2
        var page1 = await listHandler.HandleAsync(new ListReservationsQuery(PageSize: 2, PageNumber: 1));
        Assert.True(page1.IsSuccess);
        Assert.NotNull(page1.Data);
        Assert.Equal(2, page1.Data.Items.Count);
        Assert.Equal(5, page1.Data.TotalCount);
        Assert.Equal(3, page1.Data.TotalPages);
        Assert.False(page1.Data.HasPreviousPage);
        Assert.True(page1.Data.HasNextPage);

        // Page 3 should have 1 item
        var page3 = await listHandler.HandleAsync(new ListReservationsQuery(PageSize: 2, PageNumber: 3));
        Assert.True(page3.IsSuccess);
        Assert.Single(page3.Data!.Items);
        Assert.True(page3.Data.HasPreviousPage);
        Assert.False(page3.Data.HasNextPage);
    }

    [Fact]
    public async Task ListReservations_WithInvalidPagination_ReturnsFailure()
    {
        var reservations = new InMemoryReservationRepository();
        var handler = new ListReservationsHandler(reservations);

        var result = await handler.HandleAsync(new ListReservationsQuery(PageNumber: 0));
        Assert.True(result.IsFailure);

        result = await handler.HandleAsync(new ListReservationsQuery(PageSize: 0));
        Assert.True(result.IsFailure);

        result = await handler.HandleAsync(new ListReservationsQuery(PageSize: 51));
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task ListReservations_WithSnakeCaseStatusFilter_ReturnsFiltered()
    {
        var (events, reservations, _, clock, eventId) = HandlerTestSetup.CreateWithSeed();

        var reserveHandler = new ReserveTicketsHandler(events, reservations, clock);
        await reserveHandler.HandleAsync(new ReserveTicketsRequest(eventId, 2, "Alice", "alice@test.com"));
        await reserveHandler.HandleAsync(new ReserveTicketsRequest(eventId, 3, "Bob", "bob@test.com"));

        var listHandler = new ListReservationsHandler(reservations);

        // Use snake_case as the frontend does
        var result = await listHandler.HandleAsync(
            new ListReservationsQuery(Status: "pendiente_pago"));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.Items.Count);
        Assert.All(result.Data.Items, r => Assert.Equal("pendiente_pago", r.Status));
    }

    [Fact]
    public async Task ListReservations_WithPascalCaseStatusFilter_ReturnsFiltered()
    {
        var (events, reservations, _, clock, eventId) = HandlerTestSetup.CreateWithSeed();

        var reserveHandler = new ReserveTicketsHandler(events, reservations, clock);
        await reserveHandler.HandleAsync(new ReserveTicketsRequest(eventId, 2, "Alice", "alice@test.com"));
        await reserveHandler.HandleAsync(new ReserveTicketsRequest(eventId, 3, "Bob", "bob@test.com"));

        var listHandler = new ListReservationsHandler(reservations);

        // PascalCase enum name should also work
        var result = await listHandler.HandleAsync(
            new ListReservationsQuery(Status: "PendientePago"));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.Items.Count);
    }

    [Fact]
    public async Task ListReservations_WithInvalidStatus_ReturnsFailure()
    {
        var reservations = new InMemoryReservationRepository();
        var handler = new ListReservationsHandler(reservations);

        var result = await handler.HandleAsync(
            new ListReservationsQuery(Status: "invalid_status"));

        Assert.True(result.IsFailure);
        Assert.Contains("Invalid status", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    // ─── GetReservationHandler Tests ───────────────────────────────────────

    [Fact]
    public async Task GetReservation_WithExistingId_ReturnsReservation()
    {
        var (events, reservations, _, clock, eventId) = HandlerTestSetup.CreateWithSeed();

        var reserveHandler = new ReserveTicketsHandler(events, reservations, clock);
        var reserveResult = await reserveHandler.HandleAsync(
            new ReserveTicketsRequest(eventId, 2, "Get Test", "get@test.com"));
        Assert.True(reserveResult.IsSuccess);

        var getHandler = new GetReservationHandler(reservations);
        var result = await getHandler.HandleAsync(new GetReservationQuery(reserveResult.Data!.Id));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(reserveResult.Data.Id, result.Data.Id);
    }

    [Fact]
    public async Task GetReservation_WithNonexistentId_ReturnsFailure()
    {
        var reservations = new InMemoryReservationRepository();
        var handler = new GetReservationHandler(reservations);

        var result = await handler.HandleAsync(new GetReservationQuery(Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.Contains("not found", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    // ─── UpdateReservationHandler Tests ─────────────────────────────────────

    [Fact]
    public async Task UpdateReservation_WithValidData_ReturnsUpdatedReservation()
    {
        var (events, reservations, _, clock, eventId) = HandlerTestSetup.CreateWithSeed();

        // Create a pending reservation
        var reserveHandler = new ReserveTicketsHandler(events, reservations, clock);
        var reserveResult = await reserveHandler.HandleAsync(
            new ReserveTicketsRequest(eventId, 3, "Original Buyer", "original@test.com"));
        Assert.True(reserveResult.IsSuccess);
        var reservationId = reserveResult.Data!.Id;

        // Update the reservation
        var updateHandler = new UpdateReservationHandler(reservations, events, clock);
        var updateResult = await updateHandler.HandleAsync(
            new UpdateReservationRequest(reservationId, 5, "Updated Buyer", "updated@test.com"));

        Assert.True(updateResult.IsSuccess);
        Assert.NotNull(updateResult.Data);
        Assert.Equal(reservationId, updateResult.Data.Id);
        Assert.Equal(5, updateResult.Data.Quantity);
        Assert.Equal("Updated Buyer", updateResult.Data.BuyerName);
        Assert.Equal("updated@test.com", updateResult.Data.BuyerEmail);
        Assert.Equal("pendiente_pago", updateResult.Data.Status);
    }

    [Fact]
    public async Task UpdateReservation_OnConfirmedReservation_ReturnsFailure()
    {
        var (events, reservations, _, clock, eventId) = HandlerTestSetup.CreateWithSeed();

        // Create a pending reservation then confirm it
        var reserveHandler = new ReserveTicketsHandler(events, reservations, clock);
        var reserveResult = await reserveHandler.HandleAsync(
            new ReserveTicketsRequest(eventId, 2, "Confirmed Buyer", "confirmed@test.com"));
        Assert.True(reserveResult.IsSuccess);

        var confirmHandler = new ConfirmPaymentHandler(reservations, clock);
        await confirmHandler.HandleAsync(new ConfirmPaymentRequest(reserveResult.Data!.Id));

        // Attempt to update the confirmed reservation
        var updateHandler = new UpdateReservationHandler(reservations, events, clock);
        var updateResult = await updateHandler.HandleAsync(
            new UpdateReservationRequest(reserveResult.Data!.Id, 3, "New Name", "new@test.com"));

        Assert.True(updateResult.IsFailure);
        Assert.Contains("Only pending-payment", updateResult.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateReservation_OnExpiredPendingReservation_ReturnsFailure()
    {
        var (events, reservations, _, clock, eventId) = HandlerTestSetup.CreateWithSeed();

        var reserveHandler = new ReserveTicketsHandler(events, reservations, clock);
        var reserveResult = await reserveHandler.HandleAsync(
            new ReserveTicketsRequest(eventId, 2, "Expired Edit", "expired-edit@test.com"));
        Assert.True(reserveResult.IsSuccess);

        var expiredClock = new FakeClock(clock.UtcNow.AddMinutes(16));
        var updateHandler = new UpdateReservationHandler(reservations, events, expiredClock);
        var updateResult = await updateHandler.HandleAsync(
            new UpdateReservationRequest(reserveResult.Data!.Id, 3, "New Name", "new@test.com"));

        Assert.True(updateResult.IsFailure);
        Assert.Contains("expired", updateResult.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateReservation_WithExcessQuantity_ReturnsFailure()
    {
        var (events, reservations, _, clock, eventId) = HandlerTestSetup.CreateWithSeed();

        // Create a pending reservation
        var reserveHandler = new ReserveTicketsHandler(events, reservations, clock);
        var reserveResult = await reserveHandler.HandleAsync(
            new ReserveTicketsRequest(eventId, 2, "Capacity Test", "capacity@test.com"));
        Assert.True(reserveResult.IsSuccess);

        // Fill remaining capacity with confirmed reservations
        var confirmHandler = new ConfirmPaymentHandler(reservations, clock);
        var r2 = await reserveHandler.HandleAsync(
            new ReserveTicketsRequest(eventId, 90, "Filler", "filler@test.com"));
        Assert.True(r2.IsSuccess);
        await confirmHandler.HandleAsync(new ConfirmPaymentRequest(r2.Data!.Id));

        // Attempt to update the pending reservation to more than available
        // Event capacity=100, confirmed=90, pending=2 (original), so adjusted pending=0, available=10
        var updateHandler = new UpdateReservationHandler(reservations, events, clock);
        var updateResult = await updateHandler.HandleAsync(
            new UpdateReservationRequest(reserveResult.Data!.Id, 15, "Overflow", "overflow@test.com"));

        Assert.True(updateResult.IsFailure);
        Assert.Contains("available", updateResult.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateReservation_WithInvalidEmail_ReturnsFailure()
    {
        var (events, reservations, _, clock, eventId) = HandlerTestSetup.CreateWithSeed();

        // Create a pending reservation
        var reserveHandler = new ReserveTicketsHandler(events, reservations, clock);
        var reserveResult = await reserveHandler.HandleAsync(
            new ReserveTicketsRequest(eventId, 2, "Email Test", "email@test.com"));
        Assert.True(reserveResult.IsSuccess);

        // Attempt to update with invalid email
        var updateHandler = new UpdateReservationHandler(reservations, events, clock);
        var updateResult = await updateHandler.HandleAsync(
            new UpdateReservationRequest(reserveResult.Data!.Id, 3, "New Name", "not-an-email"));

        Assert.True(updateResult.IsFailure);
    }

    [Fact]
    public async Task UpdateReservation_OnNonexistentReservation_ReturnsFailure()
    {
        var events = new InMemoryEventRepository();
        var reservations = new InMemoryReservationRepository();
        var clock = HandlerTestSetup.DefaultClock;

        var handler = new UpdateReservationHandler(reservations, events, clock);
        var result = await handler.HandleAsync(
            new UpdateReservationRequest(Guid.NewGuid(), 2, "Nobody", "nobody@test.com"));

        Assert.True(result.IsFailure);
        Assert.Contains("not found", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateReservation_OnCanceledReservation_ReturnsFailure()
    {
        var (events, reservations, _, clock, eventId) = HandlerTestSetup.CreateWithSeed();

        // Create a pending reservation then cancel it
        var reserveHandler = new ReserveTicketsHandler(events, reservations, clock);
        var reserveResult = await reserveHandler.HandleAsync(
            new ReserveTicketsRequest(eventId, 2, "Canceled Buyer", "canceled@test.com"));
        Assert.True(reserveResult.IsSuccess);

        var cancelHandler = new CancelReservationHandler(reservations, events, clock);
        await cancelHandler.HandleAsync(new CancelReservationRequest(reserveResult.Data!.Id));

        // Attempt to update the canceled reservation
        var updateHandler = new UpdateReservationHandler(reservations, events, clock);
        var updateResult = await updateHandler.HandleAsync(
            new UpdateReservationRequest(reserveResult.Data!.Id, 3, "New Name", "new@test.com"));

        Assert.True(updateResult.IsFailure);
        Assert.Contains("Only pending-payment", updateResult.Error, StringComparison.OrdinalIgnoreCase);
    }
}
