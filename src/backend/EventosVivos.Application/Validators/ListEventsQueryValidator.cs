using FluentValidation;
using EventosVivos.Application.Features.Events.Queries.ListEvents;

namespace EventosVivos.Application.Validators;

/// <summary>
/// Validates event listing query parameters.
/// </summary>
public class ListEventsQueryValidator : AbstractValidator<ListEventsQuery>
{
    public ListEventsQueryValidator()
    {
        When(x => x.Type is not null, () =>
        {
            RuleFor(x => x.Type!)
                .Must(t => t is "conferencia" or "taller" or "concierto")
                .WithMessage("Filter by type must be conferencia, taller, or concierto.");
        });

        When(x => x.Status is not null, () =>
        {
            RuleFor(x => x.Status!)
                .Must(s => s is "activo" or "cancelado" or "completado")
                .WithMessage("Filter by status must be activo, cancelado, or completado.");
        });

        When(x => x.StartsAtFrom is not null && x.StartsAtTo is not null, () =>
        {
            RuleFor(x => x.StartsAtTo!)
                .Must((query, to) => to > query.StartsAtFrom)
                .WithMessage("End date range must be after start date range.");
        });
    }
}
