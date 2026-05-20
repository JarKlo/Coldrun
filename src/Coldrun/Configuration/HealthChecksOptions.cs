namespace Coldrun.Configuration;

/// <summary>
/// Health checks configuration options.
/// </summary>
public sealed class HealthChecksOptions
{
    /// <summary>
    /// When true, readiness check includes database connectivity verification.
    /// Set to false for MVP (in-memory store); set to true in Phase 2+ with PostgreSQL.
    /// </summary>
    public bool EnableReadinessDbCheck { get; set; } = false;
}
