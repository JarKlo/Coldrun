namespace Coldrun.Configuration;

/// <summary>
/// Root configuration options for the Coldrun ERP application.
/// Bound from the "Coldrun" section in appsettings.json.
/// </summary>
public sealed class ColdrunOptions
{
    public const string SectionName = "Coldrun";

    public RateLimitingOptions RateLimiting { get; set; } = new();

    public HealthChecksOptions HealthChecks { get; set; } = new();

    public ModuleOptions Modules { get; set; } = new();
}
