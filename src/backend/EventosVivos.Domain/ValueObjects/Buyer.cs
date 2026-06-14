using System.Text.RegularExpressions;
using EventosVivos.Domain.Rules;

namespace EventosVivos.Domain.ValueObjects;

public partial record Buyer
{
    public string Name { get; }
    public string Email { get; }

    public Buyer(string name, string email)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length < ReservationRules.BuyerNameMinLength || name.Length > ReservationRules.BuyerNameMaxLength)
            throw new ArgumentException($"Name must be between {ReservationRules.BuyerNameMinLength} and {ReservationRules.BuyerNameMaxLength} characters.", nameof(name));

        if (email.Length > ReservationRules.BuyerEmailMaxLength)
            throw new ArgumentException($"Email must not exceed {ReservationRules.BuyerEmailMaxLength} characters.", nameof(email));

        if (!EmailRegex().IsMatch(email))
            throw new ArgumentException("Email is not valid.", nameof(email));

        Name = name.Trim();
        Email = email.Trim();
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();
}
