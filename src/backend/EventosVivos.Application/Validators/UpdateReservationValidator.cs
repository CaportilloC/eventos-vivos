using FluentValidation;
using EventosVivos.Application.Handlers;

namespace EventosVivos.Application.Validators;

/// <summary>
/// Validates reservation update input before business-rule evaluation.
/// Matches the same rules as <see cref="ReserveTicketsValidator"/>.
/// </summary>
public class UpdateReservationValidator : AbstractValidator<UpdateReservationRequest>
{
    public UpdateReservationValidator()
    {
        RuleFor(x => x.ReservationId)
            .NotEmpty().WithMessage("Reservation ID is required.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be at least 1.")
            .LessThanOrEqualTo(100).WithMessage("Quantity must not exceed 100.");

        RuleFor(x => x.BuyerName)
            .NotEmpty().WithMessage("Buyer name is required.")
            .MinimumLength(2).WithMessage("Buyer name must be at least 2 characters.")
            .MaximumLength(100).WithMessage("Buyer name must not exceed 100 characters.");

        RuleFor(x => x.BuyerEmail)
            .NotEmpty().WithMessage("Buyer email is required.")
            .EmailAddress().WithMessage("Buyer email is not valid.")
            .MaximumLength(200).WithMessage("Buyer email must not exceed 200 characters.");
    }
}
