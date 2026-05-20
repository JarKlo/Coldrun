namespace Coldrun.Configuration;

/// <summary>
/// Rate limiting configuration options.
/// </summary>
public sealed class RateLimitingOptions
{
    /// <summary>
    /// Maximum number of requests permitted within the window.
    /// </summary>
    public int PermitLimit { get; set; } = 100;

    /// <summary>
    /// Window duration in minutes.
    /// </summary>
    public int WindowMinutes { get; set; } = 1;
}
