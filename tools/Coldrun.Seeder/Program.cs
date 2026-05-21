using Coldrun.Seeder.Services;

// ── CLI Argument Parsing ──────────────────────────────────────
string? jsonFile = null;
string? scenarioFile = null;
string baseUrl = "http://localhost:5000";
bool reset = false;

var cmdArgs = Environment.GetCommandLineArgs();
for (int i = 0; i < cmdArgs.Length; i++)
{
    switch (cmdArgs[i])
    {
        case "--file":
            if (i + 1 < cmdArgs.Length)
                jsonFile = cmdArgs[++i];
            break;
        case "--scenario":
            if (i + 1 < cmdArgs.Length)
                scenarioFile = cmdArgs[++i];
            break;
        case "--baseUrl":
            if (i + 1 < cmdArgs.Length)
                baseUrl = cmdArgs[++i];
            break;
        case "--reset":
            reset = true;
            break;
    }
}

// ── Environment variable override for base URL ────────────────
var envUrl = Environment.GetEnvironmentVariable("COLDRUN_API_URL");
if (!string.IsNullOrWhiteSpace(envUrl))
    baseUrl = envUrl;

// ── Validate: exactly one of --file or --scenario ─────────────
if (string.IsNullOrWhiteSpace(jsonFile) && string.IsNullOrWhiteSpace(scenarioFile))
{
    Console.WriteLine("Usage: Coldrun.Seeder --file <path> [--baseUrl <url>] [--reset]");
    Console.WriteLine("       Coldrun.Seeder --scenario <path> [--baseUrl <url>] [--reset]");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  --file      Path to JSON seed data file");
    Console.WriteLine("  --scenario  Path to E2E scenario file");
    Console.WriteLine("  --baseUrl   API base URL (default: http://localhost:5000)");
    Console.WriteLine("  --reset     Delete all existing trucks before seeding");
    Console.WriteLine();
    Console.WriteLine("Environment:");
    Console.WriteLine("  COLDRUN_API_URL   Overrides --baseUrl default");
    return 1;
}

if (!string.IsNullOrWhiteSpace(jsonFile) && !string.IsNullOrWhiteSpace(scenarioFile))
{
    Console.WriteLine("[ERROR] Cannot use both --file and --scenario. Choose one.");
    return 1;
}

if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uriResult) || uriResult.Scheme is not ("http" or "https"))
{
    Console.WriteLine($"[ERROR] Invalid base URL: {baseUrl}");
    return 1;
}

// ── Configure HttpClient ──────────────────────────────────────
using var httpClient = new HttpClient
{
    BaseAddress = uriResult,
    Timeout = TimeSpan.FromSeconds(30)
};

// ── Health Check ──────────────────────────────────────────────
Console.WriteLine($"Checking API health at {baseUrl}...");
var seeder = new ApiSeeder(httpClient);
var isHealthy = await seeder.HealthCheckAsync();
if (!isHealthy)
{
    Console.WriteLine("[ERROR] API is not running or health check failed.");
    Console.WriteLine("Please start the Coldrun API before running the seeder.");
    return 1;
}
Console.WriteLine("API is healthy.");
Console.WriteLine();

// ── Reset (if requested) ──────────────────────────────────────
if (reset)
{
    Console.WriteLine("Resetting: deleting all existing trucks...");
    await seeder.ResetAsync();
    Console.WriteLine("Reset complete.");
    Console.WriteLine();
}

// ── Branch: Data Seeding vs E2E Scenarios ─────────────────────
if (!string.IsNullOrWhiteSpace(scenarioFile))
{
    // ── E2E Scenario Mode ─────────────────────────────────────
    if (!File.Exists(scenarioFile))
    {
        Console.WriteLine($"[ERROR] Scenario file not found: {scenarioFile}");
        return 1;
    }

    Console.WriteLine($"Running E2E scenarios from: {scenarioFile}");
    Console.WriteLine();

    var runner = new ScenarioRunner(httpClient);
    var results = await runner.RunScenariosAsync(scenarioFile);

    // ── Summary ───────────────────────────────────────────────
    Console.WriteLine("═".PadRight(50, '═'));
    Console.WriteLine("E2E Scenario Results");
    Console.WriteLine("═".PadRight(50, '═'));

    int totalPassed = 0;
    int totalFailed = 0;
    int scenariosPassed = 0;
    int scenariosFailed = 0;

    foreach (var result in results)
    {
        Console.WriteLine();
        Console.WriteLine($"[{(result.Passed ? "PASS" : "FAIL")}] {result.ScenarioId}: {result.ScenarioName}");
        Console.WriteLine($"  Steps: {result.PassedSteps} passed, {result.FailedSteps} failed");

        if (!result.Passed)
        {
            foreach (var stepResult in result.StepResults.Where(s => !s.Passed))
            {
                Console.WriteLine($"    Step {stepResult.StepNumber}: {stepResult.TruckCode} → {stepResult.TargetStatus}");
                Console.WriteLine($"      Expected: HTTP {stepResult.ExpectedStatusCode}" +
                    (stepResult.ExpectedStatus != null ? $", Status '{stepResult.ExpectedStatus}'" : ""));
                Console.WriteLine($"      Actual:   HTTP {stepResult.ActualStatusCode}" +
                    (stepResult.ActualStatus != null ? $", Status '{stepResult.ActualStatus}'" : ""));
                if (!string.IsNullOrWhiteSpace(stepResult.ErrorMessage))
                    Console.WriteLine($"      Error:    {stepResult.ErrorMessage}");
            }
        }

        totalPassed += result.PassedSteps;
        totalFailed += result.FailedSteps;
        if (result.Passed)
            scenariosPassed++;
        else
            scenariosFailed++;
    }

    Console.WriteLine();
    Console.WriteLine("─".PadRight(50, '─'));
    Console.WriteLine($"Scenarios: {scenariosPassed} passed, {scenariosFailed} failed, {results.Count} total");
    Console.WriteLine($"Steps:     {totalPassed} passed, {totalFailed} failed, {totalPassed + totalFailed} total");

    return scenariosFailed > 0 ? 1 : 0;
}
else
{
    // ── Data Seeding Mode (existing) ──────────────────────────
    if (!File.Exists(jsonFile))
    {
        Console.WriteLine($"[ERROR] JSON file not found: {jsonFile}");
        return 1;
    }

    Console.WriteLine($"Seeding from: {jsonFile}");
    var result = await seeder.SeedAsync(jsonFile);

    // ── Summary ───────────────────────────────────────────────
    Console.WriteLine();
    Console.WriteLine("─".PadRight(40, '─'));
    Console.WriteLine($"Seeding complete.");
    Console.WriteLine($"  Created: {result.Created}");
    Console.WriteLine($"  Skipped: {result.Skipped}");
    Console.WriteLine($"  Failed:  {result.Failed}");
    Console.WriteLine($"  Total:   {result.Created + result.Skipped + result.Failed}");

    return result.Failed > 0 ? 1 : 0;
}
