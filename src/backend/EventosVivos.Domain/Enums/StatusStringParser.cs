namespace EventosVivos.Domain.Enums;

/// <summary>
/// Utility for normalizing status filter strings from snake_case (e.g. "pendiente_pago")
/// to PascalCase (e.g. "PendientePago") so that <c>Enum.TryParse</c> can match them.
/// Handles PascalCase and all-caps snake_case as well for robustness.
/// </summary>
public static class StatusStringParser
{
    /// <summary>
    /// Normalizes a status filter string for safe use with <c>Enum.TryParse</c>.
    /// Converts snake_case to PascalCase. Passes through already-PascalCase strings unchanged.
    /// </summary>
    /// <param name="input">The raw status string, e.g. "pendiente_pago" or "PendientePago".</param>
    /// <returns>The normalized PascalCase string, or the original if null/empty.</returns>
    public static string? NormalizeToPascalCase(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        // Only convert if underscores are present (snake_case or similar)
        if (!input.Contains('_'))
            return input;

        return string.Join("", input.Split('_')
            .Where(s => s.Length > 0)
            .Select(s => char.ToUpperInvariant(s[0]) + s[1..].ToLowerInvariant()));
    }
}
