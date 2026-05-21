namespace Coldrun.Seeder.Models;

/// <summary>
/// Root JSON structure for E2E scenario files.
/// </summary>
public sealed class ScenarioFile
{
    public List<Scenario> Scenarios { get; set; } = [];
}

/// <summary>
/// A single E2E test scenario with setup and ordered steps.
/// </summary>
public sealed class Scenario
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public ScenarioSetup Setup { get; set; } = new();
    public List<Step> Steps { get; set; } = [];
}

/// <summary>
/// Setup section: trucks to create before running steps.
/// </summary>
public sealed class ScenarioSetup
{
    public List<TruckSeedEntry> Trucks { get; set; } = [];
}

/// <summary>
/// A single step in a scenario: a status transition with expected outcome.
/// </summary>
public sealed class Step
{
    public int StepNumber { get; set; }
    public string Action { get; set; } = "TransitionStatus";
    public string TruckCode { get; set; } = "";
    public string TargetStatus { get; set; } = "";
    public ExpectedOutcome ExpectedOutcome { get; set; } = new();
}

/// <summary>
/// Expected outcome for a step: HTTP status code and optionally the resulting status.
/// </summary>
public sealed class ExpectedOutcome
{
    public int HttpStatusCode { get; set; }
    public string? ResultStatus { get; set; }
}

/// <summary>
/// Result of executing a single scenario.
/// </summary>
public sealed class ScenarioResult
{
    public string ScenarioId { get; set; } = "";
    public string ScenarioName { get; set; } = "";
    public bool Passed { get; set; }
    public List<ScenarioStepResult> StepResults { get; set; } = [];
    public int PassedSteps { get; set; }
    public int FailedSteps { get; set; }
}

/// <summary>
/// Result of executing a single step within a scenario.
/// </summary>
public sealed class ScenarioStepResult
{
    public int StepNumber { get; set; }
    public string TruckCode { get; set; } = "";
    public string TargetStatus { get; set; } = "";
    public bool Passed { get; set; }
    public int ExpectedStatusCode { get; set; }
    public int ActualStatusCode { get; set; }
    public string? ExpectedStatus { get; set; }
    public string? ActualStatus { get; set; }
    public string? ErrorMessage { get; set; }
}
