using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EventosVivos.Tests.IntegrationTests;

/// <summary>
/// Integration tests that exercise the full API stack against a real SQL Server Docker instance.
///
/// PREREQUISITE: Docker SQL Server must be running:
///   docker compose -f docker/docker-compose.yml up -d
///
/// Each test class gets a clean database (CustomWebApplicationFactory drops + migrates),
/// and each test creates its own data via API calls to avoid cross-test pollution.
/// </summary>
[Collection("IntegrationTests")]
public class ApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public ApiIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            // Do not follow redirects — we want to check response codes directly
            AllowAutoRedirect = false,
        });
    }

    // ── DTOs for API responses (matching server shapes) ───────────────────

    private record VenueResponse(int Id, string Name, int Capacity, string City);

    private record EventResponse(
        Guid Id,
        string Title,
        string? Description,
        string Type,
        int VenueId,
        string Status,
        DateTimeOffset StartsAt,
        DateTimeOffset EndsAt,
        decimal Price,
        int MaxCapacity);

    private record ReservationResponse(
        Guid Id,
        Guid EventId,
        int Quantity,
        string Status,
        string BuyerName,
        string BuyerEmail,
        DateTimeOffset CreatedAt,
        DateTimeOffset? ExpiresAt,
        DateTimeOffset? ConfirmedAt,
        DateTimeOffset? CanceledAt,
        string? Code);

    private record PagedResultResponse<T>(
        IReadOnlyList<T> Items,
        int PageNumber,
        int PageSize,
        int TotalCount,
        int TotalPages,
        bool HasPreviousPage,
        bool HasNextPage);

    private record OccupancyReportResponse(
        Guid EventId,
        string Status,
        int ConfirmedTickets,
        int LostTickets,
        int AvailableTickets,
        decimal OccupancyPercentage,
        decimal Revenue);

    // ── Request helpers ───────────────────────────────────────────────────

    private static StringContent ToJson(object value)
    {
        var json = JsonSerializer.Serialize(value, JsonOptions);
        return new StringContent(json, System.Text.Encoding.UTF8, "application/json");
    }

    private static async Task<T> ReadAsAsync<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<T>(content, JsonOptions);
        return result ?? throw new InvalidOperationException("Failed to deserialize response");
    }

    // ── Test helpers ──────────────────────────────────────────────────────

    private static int _eventSlotOffset;

    /// <summary>
    /// Creates an event and returns its ID.
    /// Uses venue 1 (Auditorio Central, capacity 200) with a future date.
    /// Each call uses a unique time slot to avoid venue-overlap conflicts.
    /// </summary>
    private async Task<Guid> CreateEventAsync()
    {
        var offset = Interlocked.Increment(ref _eventSlotOffset);
        var baseDate = new DateTimeOffset(2026, 8, 15, 14, 0, 0, TimeSpan.Zero);
        var futureStart = baseDate.AddDays(offset * 7);  // unique week slot per call
        var futureEnd = futureStart.AddHours(2);

        var request = new
        {
            Title = $"Integration Test Event - {Guid.NewGuid():N}"[..30],
            Description = "Created by integration test",
            VenueId = 1,
            MaxCapacity = 100,
            StartsAt = futureStart,
            EndsAt = futureEnd,
            Price = 50m,
            Type = "conferencia",
        };

        var response = await _client.PostAsync("/api/v1/events", ToJson(request));
        var body = await response.Content.ReadAsStringAsync();

        if (response.StatusCode != HttpStatusCode.Created)
        {
            throw new InvalidOperationException(
                $"CreateEventAsync failed with {response.StatusCode}: {body}");
        }

        return JsonSerializer.Deserialize<Guid>(body, JsonOptions);
    }

    private async Task<HttpResponseMessage> CreateEventResponseAsync()
    {
        var offset = Interlocked.Increment(ref _eventSlotOffset);
        var futureStart = new DateTimeOffset(2026, 8, 15, 14, 0, 0, TimeSpan.Zero).AddDays(offset * 7);

        var request = new
        {
            Title = $"Location Header Event - {Guid.NewGuid():N}"[..30],
            Description = "Created by integration test",
            VenueId = 1,
            MaxCapacity = 100,
            StartsAt = futureStart,
            EndsAt = futureStart.AddHours(2),
            Price = 50m,
            Type = "conferencia",
        };

        return await _client.PostAsync("/api/v1/events", ToJson(request));
    }

    /// <summary>
    /// Creates a pending reservation for the given event.
    /// </summary>
    private async Task<ReservationResponse> ReserveTicketsAsync(Guid eventId, int quantity)
    {
        var request = new
        {
            EventId = eventId,
            Quantity = quantity,
            BuyerName = "Test Buyer",
            BuyerEmail = "buyer@test.com",
        };

        var response = await _client.PostAsync("/api/v1/reservations", ToJson(request));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        return await ReadAsAsync<ReservationResponse>(response);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  TEST 1: Seed venues are present after migration
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SeedVenues_ArePresentAfterMigration()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/venues");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var venuesResult = await ReadAsAsync<PagedResultResponse<VenueResponse>>(response);
        Assert.NotEmpty(venuesResult.Items);

        // Verify the 3 PDF-seeded venues
        var auditorio = Assert.Single(venuesResult.Items, v => v.Id == 1);
        Assert.Equal("Auditorio Central", auditorio.Name);
        Assert.Equal(200, auditorio.Capacity);
        Assert.Equal("Bogotá", auditorio.City);

        var sala = Assert.Single(venuesResult.Items, v => v.Id == 2);
        Assert.Equal("Sala Norte", sala.Name);
        Assert.Equal(50, sala.Capacity);

        var arena = Assert.Single(venuesResult.Items, v => v.Id == 3);
        Assert.Equal("Arena Sur", arena.Name);
        Assert.Equal(500, arena.Capacity);
        Assert.Equal("Medellín", arena.City);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  TEST 2: Create event through API
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CreateEvent_ThroughApi_PersistsAndReturnsId()
    {
        // Act
        var eventId = await CreateEventAsync();

        // Assert — the event exists in the database
        Assert.NotEqual(Guid.Empty, eventId);

        // List events to verify persistence
        var listResponse = await _client.GetAsync("/api/v1/events");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var eventsResult = await ReadAsAsync<PagedResultResponse<EventResponse>>(listResponse);
        var created = Assert.Single(eventsResult.Items, e => e.Id == eventId);
        Assert.NotNull(created);
        Assert.Equal("conferencia", created.Type);
        Assert.Equal("activo", created.Status);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CreateEvent_ThroughApi_ReturnsLocationHeader()
    {
        var response = await CreateEventResponseAsync();
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var eventId = await ReadAsAsync<Guid>(response);

        Assert.NotNull(response.Headers.Location);
        Assert.EndsWith($"/api/v1/events/{eventId}", response.Headers.Location!.ToString());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CreateEvent_WithInvalidCapacity_ReturnsBadRequest()
    {
        // Arrange — venue 2 has capacity 50
        var futureStart = new DateTimeOffset(2026, 8, 15, 14, 0, 0, TimeSpan.Zero);
        var futureEnd = new DateTimeOffset(2026, 8, 15, 16, 0, 0, TimeSpan.Zero);

        var request = new
        {
            Title = "Over Capacity Event",
            Description = "Valid event description",
            VenueId = 2,   // Sala Norte — capacity 50
            MaxCapacity = 51, // exceeds venue capacity
            StartsAt = futureStart,
            EndsAt = futureEnd,
            Price = 30m,
            Type = "taller",
        };

        // Act
        var response = await _client.PostAsync("/api/v1/events", ToJson(request));

        // Assert — should fail with 409 (business rule conflict)
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  TEST 3: Reserve tickets and capacity holds are reflected
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ReserveTickets_ThroughApi_CreatesPendingReservation()
    {
        // Arrange — create an event first
        var eventId = await CreateEventAsync();

        // Act — reserve 3 tickets
        var reservation = await ReserveTicketsAsync(eventId, 3);

        // Assert
        Assert.NotEqual(Guid.Empty, reservation.Id);
        Assert.Equal(eventId, reservation.EventId);
        Assert.Equal(3, reservation.Quantity);
        Assert.Equal("pendiente_pago", reservation.Status);
        Assert.NotNull(reservation.ExpiresAt);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ReserveTickets_ExceedingAvailability_ReturnsConflict()
    {
        // Arrange — create an event with max capacity of 100
        var eventId = await CreateEventAsync();

        // Reserve 100 tickets (fills the event)
        await ReserveTicketsAsync(eventId, 100);

        // Act — try to reserve 1 more
        var request = new
        {
            EventId = eventId,
            Quantity = 1,
            BuyerName = "Over Buyer",
            BuyerEmail = "over@test.com",
        };
        var response = await _client.PostAsync("/api/v1/reservations", ToJson(request));

        // Assert — capacity exceeded
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  TEST 4: Confirm payment changes reservation status and generates code
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ConfirmPayment_ThroughApi_ChangesStatusAndGeneratesCode()
    {
        // Arrange — create event + reserve tickets
        var eventId = await CreateEventAsync();
        var reservation = await ReserveTicketsAsync(eventId, 2);

        // Act — confirm payment
        var confirmResponse = await _client.PostAsync(
            $"/api/v1/reservations/{reservation.Id}/confirm-payment", null);
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);

        var confirmed = await ReadAsAsync<ReservationResponse>(confirmResponse);

        // Assert
        Assert.Equal(reservation.Id, confirmed.Id);
        Assert.Equal("confirmada", confirmed.Status);
        Assert.NotNull(confirmed.Code);
        Assert.StartsWith("EV-", confirmed.Code);
        Assert.True(confirmed.Code.Length == 9); // "EV-" + 6 digits
        Assert.NotNull(confirmed.ConfirmedAt);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ConfirmPayment_OnNonexistentReservation_ReturnsNotFound()
    {
        // Act
        var response = await _client.PostAsync(
            $"/api/v1/reservations/{Guid.NewGuid()}/confirm-payment", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  TEST 5: Occupancy report calculations
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    [Trait("Category", "Integration")]
    public async Task OccupancyReport_WithConfirmedReservations_ReturnsCorrectMetrics()
    {
        // Arrange — create event (capacity 100, price 50)
        var eventId = await CreateEventAsync();

        // 2 confirmed reservations: 2 + 3 = 5 tickets
        var r1 = await ReserveTicketsAsync(eventId, 2);
        await _client.PostAsync($"/api/v1/reservations/{r1.Id}/confirm-payment", null);

        var r2 = await ReserveTicketsAsync(eventId, 3);
        await _client.PostAsync($"/api/v1/reservations/{r2.Id}/confirm-payment", null);

        // 1 pending reservation (not confirmed — should count as hold)
        var r3 = await ReserveTicketsAsync(eventId, 2);

        // Act — get occupancy report
        var reportResponse = await _client.GetAsync(
            $"/api/v1/events/{eventId}/occupancy-report");
        Assert.Equal(HttpStatusCode.OK, reportResponse.StatusCode);

        var report = await ReadAsAsync<OccupancyReportResponse>(reportResponse);

        // Assert
        Assert.Equal(eventId, report.EventId);
        Assert.Equal("activo", report.Status);
        Assert.Equal(5, report.ConfirmedTickets);       // 2 + 3 confirmed
        Assert.Equal(0, report.LostTickets);             // no canceled
        // Available: capacity(100) - confirmed(5) - pending(2) = 93
        Assert.Equal(93, report.AvailableTickets);

        // Revenue: price(50) * confirmed(5) = 250
        Assert.Equal(250m, report.Revenue);

        // Occupancy: 5/100 * 100 = 5%
        Assert.Equal(5.0m, report.OccupancyPercentage);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task OccupancyReport_ForNonexistentEvent_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync(
            $"/api/v1/events/{Guid.NewGuid()}/occupancy-report");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
