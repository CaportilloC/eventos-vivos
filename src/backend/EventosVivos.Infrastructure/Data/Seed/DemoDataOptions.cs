namespace EventosVivos.Infrastructure.Data.Seed;

public sealed class DemoDataOptions
{
    public const string SectionName = "DemoData";

    public bool SeedOnStartup { get; init; }
    public bool ResetBeforeSeed { get; init; }
}
