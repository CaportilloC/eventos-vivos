using FluentValidation;
using EventosVivos.Application.Handlers;
using EventosVivos.Domain.ValueObjects;

namespace EventosVivos.Application.Validators;

/// <summary>
/// Validates event creation input before business-rule evaluation.
/// </summary>
public class CreateEventValidator : AbstractValidator<CreateEventRequest>
{
    public CreateEventValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Event title is required.")
            .MinimumLength(5).WithMessage("Event title must be at least 5 characters.")
            .MaximumLength(100).WithMessage("Event title must not exceed 100 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Event description is required.")
            .MinimumLength(10).WithMessage("Event description must be at least 10 characters.")
            .MaximumLength(500).WithMessage("Event description must not exceed 500 characters.");

        RuleFor(x => x.VenueId)
            .GreaterThan(0).WithMessage("A valid venue must be selected.");

        RuleFor(x => x.MaxCapacity)
            .GreaterThan(0).WithMessage("Event capacity must be positive.")
            .LessThanOrEqualTo(10_000).WithMessage("Event capacity seems unreasonably high.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Ticket price must be positive.")
            .LessThanOrEqualTo(Money.MaxAmount).WithMessage("Ticket price seems unreasonably high.");

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Event type is required.")
            .Must(t => t is "conferencia" or "taller" or "concierto")
            .WithMessage("Event type must be conferencia, taller, or concierto.");

        RuleFor(x => x.StartsAt)
            .NotEmpty().WithMessage("Event start time is required.");

        RuleFor(x => x.EndsAt)
            .NotEmpty().WithMessage("Event end time is required.");

        RuleFor(x => x)
            .Must(x => x.EndsAt > x.StartsAt)
            .WithMessage("Event end must be after start.")
            .Must(x => x.StartsAt > DateTimeOffset.UtcNow)
            .WithMessage("Event start must be in the future.")
            .When(x => x.EndsAt != default && x.StartsAt != default);
    }
}
