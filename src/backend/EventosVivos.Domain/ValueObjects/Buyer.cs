using System.Text.RegularExpressions;

namespace EventosVivos.Domain.ValueObjects;

public partial record Buyer
{
    public string Name { get; }
    public string Email { get; }

    public Buyer(string name, string email)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length < 2 || name.Length > 100)
            throw new ArgumentException("Name must be between 2 and 100 characters.", nameof(name));
        if (!EmailRegex().IsMatch(email))
            throw new ArgumentException("Email is not valid.", nameof(email));

        Name = name.Trim();
        Email = email.Trim();
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();
}
