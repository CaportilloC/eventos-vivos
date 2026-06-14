using EventosVivos.Domain;
using EventosVivos.Domain.Enums;
using EventosVivos.Domain.Policies;
using EventosVivos.Domain.Services;
using EventosVivos.Domain.ValueObjects;

namespace EventosVivos.Tests;

public class EventCreationPolicyTests
{
    private static readonly int VenueCapacity = 50;
    private static readonly Money ValidPrice = new(20);
    private static readonly string ValidTitle = "Test Event";
    private static readonly string ValidDescription = "Valid event description";

    [Fact]
    public void CreateEvent_WithValidData_Succeeds()
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 6, 10, 14, 0, 0, TimeSpan.Zero));
        var futureStart = new DateTimeOffset(2026, 6, 20, 14, 0, 0, TimeSpan.Zero);
        var futureEnd = new DateTimeOffset(2026, 6, 20, 16, 0, 0, TimeSpan.Zero);
        var schedule = new EventSchedule(futureStart, futureEnd);

        var result = EventCreationPolicy.Validate(ValidTitle, ValidDescription, 50, VenueCapacity, schedule, clock);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void CreateEvent_WithEmptyTitle_Fails()
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 6, 10, 14, 0, 0, TimeSpan.Zero));
        var futureStart = new DateTimeOffset(2026, 6, 20, 14, 0, 0, TimeSpan.Zero);
        var futureEnd = new DateTimeOffset(2026, 6, 20, 16, 0, 0, TimeSpan.Zero);
        var schedule = new EventSchedule(futureStart, futureEnd);

        var result = EventCreationPolicy.Validate("", ValidDescription, 50, VenueCapacity, schedule, clock);

        Assert.True(result.IsFailure);
        Assert.Contains("title", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    // RN-01: Event capacity cannot exceed venue capacity
    [Fact]
    public void CreateEvent_WithCapacityExceedingVenue_Fails()
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 6, 10, 14, 0, 0, TimeSpan.Zero));
        var futureStart = new DateTimeOffset(2026, 6, 20, 14, 0, 0, TimeSpan.Zero);
        var futureEnd = new DateTimeOffset(2026, 6, 20, 16, 0, 0, TimeSpan.Zero);
        var schedule = new EventSchedule(futureStart, futureEnd);

        var result = EventCreationPolicy.Validate(ValidTitle, ValidDescription, 51, 50, schedule, clock);

        Assert.True(result.IsFailure);
        Assert.Contains("cannot exceed", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CreateEvent_WithCapacityEqualToVenue_Succeeds()
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 6, 10, 14, 0, 0, TimeSpan.Zero));
        var futureStart = new DateTimeOffset(2026, 6, 20, 14, 0, 0, TimeSpan.Zero);
        var futureEnd = new DateTimeOffset(2026, 6, 20, 16, 0, 0, TimeSpan.Zero);
        var schedule = new EventSchedule(futureStart, futureEnd);

        var result = EventCreationPolicy.Validate(ValidTitle, ValidDescription, 50, 50, schedule, clock);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void CreateEvent_WithPastStart_Fails()
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 6, 20, 14, 0, 0, TimeSpan.Zero));
        var pastStart = new DateTimeOffset(2026, 6, 20, 12, 0, 0, TimeSpan.Zero);
        var futureEnd = new DateTimeOffset(2026, 6, 20, 16, 0, 0, TimeSpan.Zero);
        var schedule = new EventSchedule(pastStart, futureEnd);

        var result = EventCreationPolicy.Validate(ValidTitle, ValidDescription, 50, VenueCapacity, schedule, clock);

        Assert.True(result.IsFailure);
        Assert.Contains("future", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CreateEvent_WithEndBeforeStart_Throws()
    {
        var start = new DateTimeOffset(2026, 6, 20, 16, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2026, 6, 20, 14, 0, 0, TimeSpan.Zero);

        Assert.Throws<ArgumentException>(() => new EventSchedule(start, end));
    }

    // RN-03: Weekend events cannot start after 22:00 Colombia time
    [Theory]
    [InlineData(DayOfWeek.Saturday)]
    [InlineData(DayOfWeek.Sunday)]
    public void WeekendEvent_StartingAfter22_IsRejected(DayOfWeek weekendDay)
    {
        // Find a real date that falls on the given weekend day in Bogota
        var bogotaDate = FindNextWeekday(2026, 6, weekendDay);
        // Bogota is UTC-5
        var bogotaOffset = TimeSpan.FromHours(-5);
        // Event start 22:01 Bogota time = rejected
        var startInBogota = new DateTimeOffset(
            bogotaDate.Year, bogotaDate.Month, bogotaDate.Day, 22, 1, 0, bogotaOffset);
        var endInBogota = startInBogota.AddHours(2);

        var schedule = new EventSchedule(startInBogota, endInBogota);
        var clock = new FakeClock(startInBogota.AddDays(-7));

        var result = EventCreationPolicy.Validate(ValidTitle, ValidDescription, 50, VenueCapacity, schedule, clock);

        Assert.True(result.IsFailure);
        Assert.Contains("22:00", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(DayOfWeek.Saturday)]
    [InlineData(DayOfWeek.Sunday)]
    public void WeekendEvent_StartingAt22_IsAllowed(DayOfWeek weekendDay)
    {
        var bogotaDate = FindNextWeekday(2026, 6, weekendDay);
        var bogotaOffset = TimeSpan.FromHours(-5);
        // Event start exactly 22:00 = allowed (limit is after 22:00)
        var startInBogota = new DateTimeOffset(
            bogotaDate.Year, bogotaDate.Month, bogotaDate.Day, 22, 0, 0, bogotaOffset);
        var endInBogota = startInBogota.AddHours(2);
        var schedule = new EventSchedule(startInBogota, endInBogota);
        var clock = new FakeClock(startInBogota.AddDays(-7));

        var result = EventCreationPolicy.Validate(ValidTitle, ValidDescription, 50, VenueCapacity, schedule, clock);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void WeekdayEvent_StartingAfter22_IsAllowed()
    {
        // Pick a Wednesday in Bogota
        var bogotaOffset = TimeSpan.FromHours(-5);
        var startInBogota = new DateTimeOffset(2026, 6, 17, 22, 30, 0, bogotaOffset); // Wednesday
        var endInBogota = startInBogota.AddHours(2);
        var schedule = new EventSchedule(startInBogota, endInBogota);
        var clock = new FakeClock(startInBogota.AddDays(-7));

        var result = EventCreationPolicy.Validate(ValidTitle, ValidDescription, 50, VenueCapacity, schedule, clock);

        Assert.True(result.IsSuccess);
    }

    private static DateTime FindNextWeekday(int year, int month, DayOfWeek targetDay)
    {
        var date = new DateTime(year, month, 1);
        while (date.DayOfWeek != targetDay)
            date = date.AddDays(1);
        return date;
    }
}
