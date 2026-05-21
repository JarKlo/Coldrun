namespace Coldrun.Seeder.Services;

/// <summary>
/// Tracks seeding operation results.
/// </summary>
public sealed class SeedResult
{
    public int Created { get; set; }
    public int Skipped { get; set; }
    public int Failed { get; set; }
}
