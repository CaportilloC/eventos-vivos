using System.Security.Cryptography;

namespace EventosVivos.Domain.Policies;

/// <summary>
/// Generates reservation codes in EV-{6 digits} format.
/// </summary>
public static class ReservationCodeGenerator
{
    private const int DefaultMaxAttempts = 20;

    public static string Generate()
    {
        var digits = RandomNumberGenerator.GetInt32(0, 1_000_000);
        return $"EV-{digits:D6}";
    }

    public static async Task<string> GenerateUniqueAsync(
        Func<string, CancellationToken, Task<bool>> codeExistsAsync,
        CancellationToken ct = default,
        int maxAttempts = DefaultMaxAttempts)
    {
        ArgumentNullException.ThrowIfNull(codeExistsAsync);

        if (maxAttempts < 1)
            throw new ArgumentOutOfRangeException(nameof(maxAttempts), "Max attempts must be at least 1.");

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var code = Generate();
            if (!await codeExistsAsync(code, ct))
                return code;
        }

        throw new InvalidOperationException("Could not generate a unique reservation code after multiple attempts.");
    }
}
