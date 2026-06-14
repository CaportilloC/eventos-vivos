using EventosVivos.Domain.Enums;
using EventosVivos.Domain.Rules;
using EventosVivos.Domain.Services;
using EventosVivos.Domain.ValueObjects;

namespace EventosVivos.Domain.Policies;

/// <summary>
/// Validates event creation rules: capacity, weekend hours, future start, future end.
/// Overlap checks (RN-02) require repository access and are handled at the application layer.
/// </summary>
public static class EventCreationPolicy
{
    public static Result Validate(
        string title,
        string? description,
        int maxCapacity,
        int venueCapacity,
        EventSchedule schedule,
        IClock clock)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Result.Failure("Event title is required.", ErrorType.Validation);

        if (title.Length < EventRules.TitleMinLength || title.Length > EventRules.TitleMaxLength)
            return Result.Failure($"Event title must be between {EventRules.TitleMinLength} and {EventRules.TitleMaxLength} characters.", ErrorType.Validation);

        if (string.IsNullOrWhiteSpace(description))
            return Result.Failure("Event description is required.", ErrorType.Validation);

        if (description.Length < EventRules.DescriptionMinLength || description.Length > EventRules.DescriptionMaxLength)
            return Result.Failure($"Event description must be between {EventRules.DescriptionMinLength} and {EventRules.DescriptionMaxLength} characters.", ErrorType.Validation);

        if (maxCapacity <= 0)
            return Result.Failure("Event capacity must be positive.", ErrorType.Validation);

        // RN-01: capacity cannot exceed venue capacity
        if (maxCapacity > venueCapacity)
            return Result.Failure(
                $"Event capacity ({maxCapacity}) cannot exceed venue capacity ({venueCapacity}).");

        // Future start
        if (schedule.StartsAt <= clock.UtcNow)
            return Result.Failure("Event start must be in the future.", ErrorType.Validation);

        // RN-03: weekend events cannot start after 22:00 Colombia time
        var bogotaStart = TimeZoneInfo.ConvertTime(schedule.StartsAt, ColombiaTime.Info);
        if (IsWeekend(bogotaStart.DayOfWeek) &&
            (bogotaStart.Hour > EventRules.WeekendLatestStartHourColombia || (bogotaStart.Hour == EventRules.WeekendLatestStartHourColombia && bogotaStart.Minute > 0)))
            return Result.Failure(
                "Weekend events cannot start after 22:00 Colombia time.", ErrorType.Validation);

        return Result.Success();
    }

    private static bool IsWeekend(DayOfWeek day) =>
        day is DayOfWeek.Saturday or DayOfWeek.Sunday;
}
