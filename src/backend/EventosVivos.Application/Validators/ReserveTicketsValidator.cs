using FluentValidation;
using EventosVivos.Application.Features.Reservations.Commands.CreateReservation;
using EventosVivos.Domain.Rules;

namespace EventosVivos.Application.Validators;

/// <summary>
/// Validates ticket reservation input before business-rule evaluation.
/// </summary>
public class ReserveTicketsValidator : AbstractValidator<ReserveTicketsRequest>
{
    public ReserveTicketsValidator()
    {
        RuleFor(x => x.EventId)
            .NotEmpty().WithMessage("Event ID is required.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be at least 1.")
            .LessThanOrEqualTo(ReservationRules.MaxTicketsPerRequest).WithMessage("Quantity must not exceed 100.");

        RuleFor(x => x.BuyerName)
            .NotEmpty().WithMessage("Buyer name is required.")
            .MinimumLength(ReservationRules.BuyerNameMinLength).WithMessage("Buyer name must be at least 2 characters.")
            .MaximumLength(ReservationRules.BuyerNameMaxLength).WithMessage("Buyer name must not exceed 100 characters.");

        RuleFor(x => x.BuyerEmail)
            .NotEmpty().WithMessage("Buyer email is required.")
            .EmailAddress().WithMessage("Buyer email is not valid.")
            .MaximumLength(ReservationRules.BuyerEmailMaxLength).WithMessage("Buyer email must not exceed 200 characters.");
    }
}
