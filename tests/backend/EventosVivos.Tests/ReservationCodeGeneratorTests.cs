using EventosVivos.Domain.Policies;

namespace EventosVivos.Tests;

/// <summary>
/// Unit tests for ReservationCodeGenerator.
/// Validates format EV-{6 digits} and unique generation retry behavior.
/// </summary>
public class ReservationCodeGeneratorTests
{
    [Fact]
    public void Generate_ReturnsCodeInEV6DigitFormat()
    {
        var code = ReservationCodeGenerator.Generate();

        Assert.StartsWith("EV-", code);
        Assert.Equal(9, code.Length); // "EV-" + 6 digits
        Assert.Matches("^EV-\\d{6}$", code);
    }

    [Fact]
    public void Generate_MultipleCalls_ReturnsDifferentCodes()
    {
        var codes = new HashSet<string>();
        for (int i = 0; i < 100; i++)
        {
            var code = ReservationCodeGenerator.Generate();
            codes.Add(code);
        }

        // Statistical likelihood of all 100 being the same is negligible.
        Assert.True(codes.Count > 1,
            "Expected multiple unique reservation codes from 100 calls.");
    }

    [Fact]
    public void Generate_AllCodesMatchFormat()
    {
        for (int i = 0; i < 50; i++)
        {
            var code = ReservationCodeGenerator.Generate();
            Assert.Matches("^EV-\\d{6}$", code);
        }
    }

    [Fact]
    public void Generate_DefaultRange_AllSixDigitValues()
    {
        // The generated numeric range produces values from 0 to 999999.
        // Format always pads to 6 digits. Multiple calls verify format stability.
        for (int i = 0; i < 50; i++)
        {
            var code = ReservationCodeGenerator.Generate();
            Assert.Matches("^EV-\\d{6}$", code);
        }
    }

    [Fact]
    public async Task GenerateUniqueAsync_WhenCodesCollide_RetriesUntilUnusedCode()
    {
        var lookups = 0;

        var code = await ReservationCodeGenerator.GenerateUniqueAsync((_, _) =>
        {
            lookups++;
            return Task.FromResult(lookups < 3);
        });

        Assert.Equal(3, lookups);
        Assert.Matches("^EV-\\d{6}$", code);
    }

    [Fact]
    public async Task GenerateUniqueAsync_WhenAttemptsAreExhausted_Fails()
    {
        var result = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ReservationCodeGenerator.GenerateUniqueAsync(
                (_, _) => Task.FromResult(true),
                maxAttempts: 2));

        Assert.Contains("unique reservation code", result.Message, StringComparison.OrdinalIgnoreCase);
    }
}
