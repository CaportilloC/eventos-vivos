namespace EventosVivos.Domain.ValueObjects;

public record Money
{
    public const decimal MaxAmount = 1_000_000m;

    public decimal Amount { get; }

    public Money(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Price must be positive.", nameof(amount));
        if (amount > MaxAmount)
            throw new ArgumentException($"Price must not exceed {MaxAmount}.", nameof(amount));
        Amount = amount;
    }

    public static Money operator *(Money money, int multiplier) =>
        new(money.Amount * multiplier);
}
