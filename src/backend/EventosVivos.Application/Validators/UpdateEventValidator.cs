using FluentValidation;
using EventosVivos.Application.Features.Events.Commands.UpdateEvent;
using EventosVivos.Domain.Rules;
using EventosVivos.Domain.ValueObjects;

namespace EventosVivos.Application.Validators;

/// <summary>
/// Validates event update input before business-rule evaluation.
/// Same rules as CreateEventValidator, applied to update requests.
/// </summary>
public class UpdateEventValidator : AbstractValidator<UpdateEventRequest>
{
    public UpdateEventValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Event title is required.")
            .MinimumLength(EventRules.TitleMinLength).WithMessage("Event title must be at least 5 characters.")
            .MaximumLength(EventRules.TitleMaxLength).WithMessage("Event title must not exceed 100 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Event description is required.")
            .MinimumLength(EventRules.DescriptionMinLength).WithMessage("Event description must be at least 10 characters.")
            .MaximumLength(EventRules.DescriptionMaxLength).WithMessage("Event description must not exceed 500 characters.");

        RuleFor(x => x.VenueId)
            .GreaterThan(0).WithMessage("A valid venue must be selected.");

        RuleFor(x => x.MaxCapacity)
            .GreaterThan(0).WithMessage("Event capacity must be positive.")
            .LessThanOrEqualTo(EventRules.MaxCapacityInput).WithMessage("Event capacity seems unreasonably high.");

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
            .When(x => x.EndsAt != default && x.StartsAt != default);
    }
}
