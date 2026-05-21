using System.Net.Http.Json;
using System.Text.Json;
using Coldrun.Seeder.Models;

namespace Coldrun.Seeder.Services;

/// <summary>
/// Tracks truck data from scenario setup for use in transition steps.
/// </summary>
internal sealed class TruckContext
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Status { get; set; } = "";
    public string? Description { get; set; }
}

/// <summary>
/// Executes E2E scenarios: creates trucks, runs status transitions, validates outcomes.
/// </summary>
public sealed class ScenarioRunner(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    /// <summary>
    /// Executes all scenarios in a scenario file.
    /// </summary>
    public async Task<List<ScenarioResult>> RunScenariosAsync(
        string scenarioFilePath,
        CancellationToken ct = default)
    {
        if (!File.Exists(scenarioFilePath))
            throw new FileNotFoundException($"Scenario file not found: {scenarioFilePath}");

        var json = await File.ReadAllTextAsync(scenarioFilePath, ct);
        ScenarioFile? data;
        try
        {
            data = JsonSerializer.Deserialize<ScenarioFile>(json);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to parse scenario file '{scenarioFilePath}': {ex.Message}", ex);
        }

        if (data is null || data.Scenarios.Count == 0)
            throw new InvalidOperationException("Scenario file contains no scenarios.");

        var results = new List<ScenarioResult>();

        foreach (var scenario in data.Scenarios)
        {
            ct.ThrowIfCancellationRequested();
            var result = await RunScenarioAsync(scenario, ct);
            results.Add(result);
        }

        return results;
    }

    /// <summary>
    /// Executes a single scenario: setup + steps.
    /// </summary>
    private async Task<ScenarioResult> RunScenarioAsync(
        Scenario scenario,
        CancellationToken ct = default)
    {
        var scenarioResult = new ScenarioResult
        {
            ScenarioId = scenario.Id,
            ScenarioName = scenario.Name
        };

        Console.WriteLine($"[SCENARIO] {scenario.Id}: {scenario.Name}");
        if (!string.IsNullOrWhiteSpace(scenario.Description))
            Console.WriteLine($"  Description: {scenario.Description}");

        // Run setup — get full truck contexts
        var truckContextMap = await RunSetupAsync(scenario.Setup, ct);

        // Execute each step in order
        foreach (var step in scenario.Steps.OrderBy(s => s.StepNumber))
        {
            ct.ThrowIfCancellationRequested();
            var stepResult = await ExecuteStepAsync(step, truckContextMap, ct);
            scenarioResult.StepResults.Add(stepResult);

            if (stepResult.Passed)
                scenarioResult.PassedSteps++;
            else
                scenarioResult.FailedSteps++;
        }

        scenarioResult.Passed = scenarioResult.FailedSteps == 0;

        Console.WriteLine($"  Result: {(scenarioResult.Passed ? "PASS" : "FAIL")} ({scenarioResult.PassedSteps} passed, {scenarioResult.FailedSteps} failed)");
        Console.WriteLine();

        return scenarioResult;
    }

    /// <summary>
    /// Creates all trucks in the scenario setup.
    /// Builds a Code → TruckContext dictionary for step execution.
    /// </summary>
    private async Task<Dictionary<string, TruckContext>> RunSetupAsync(
        ScenarioSetup setup,
        CancellationToken ct = default)
    {
        var truckMap = new Dictionary<string, TruckContext>(StringComparer.OrdinalIgnoreCase);

        foreach (var truckEntry in setup.Trucks)
        {
            ct.ThrowIfCancellationRequested();

            // Check if truck already exists
            try
            {
                var existingTrucks = await _httpClient.GetFromJsonAsync<List<TruckDto>>(
                    $"api/trucks?code={Uri.EscapeDataString(truckEntry.Code)}", ct);

                if (existingTrucks is not null && existingTrucks.Count > 0)
                {
                    var existing = existingTrucks[0];
                    Console.WriteLine($"  [SETUP] Truck {truckEntry.Code} already exists (ID: {existing.Id}), using existing.");
                    truckMap[truckEntry.Code] = new TruckContext
                    {
                        Id = existing.Id,
                        Code = existing.Code,
                        Name = existing.Name,
                        Status = existing.Status,
                        Description = existing.Description
                    };
                    continue;
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"  [SETUP ERROR] Failed to check existence for '{truckEntry.Code}': {ex.Message}");
                continue;
            }

            // Create truck
            try
            {
                var createResponse = await _httpClient.PostAsJsonAsync("api/trucks",
                    new { truckEntry.Code, truckEntry.Name, truckEntry.Status, truckEntry.Description }, ct);

                if (createResponse.IsSuccessStatusCode)
                {
                    var createdTruck = await createResponse.Content.ReadFromJsonAsync<TruckDto>(ct);
                    if (createdTruck is not null)
                    {
                        Console.WriteLine($"  [SETUP] Created: {truckEntry.Code}");
                        truckMap[truckEntry.Code] = new TruckContext
                        {
                            Id = createdTruck.Id,
                            Code = createdTruck.Code,
                            Name = createdTruck.Name,
                            Status = createdTruck.Status,
                            Description = createdTruck.Description
                        };
                    }
                }
                else
                {
                    var errorContent = await createResponse.Content.ReadAsStringAsync(ct);
                    var errorMessage = ExtractErrorMessage(errorContent) ?? createResponse.StatusCode.ToString();
                    Console.WriteLine($"  [SETUP ERROR] Failed to create '{truckEntry.Code}': {errorMessage}");
                    // Don't add to map — steps using this truck will fail gracefully
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"  [SETUP ERROR] HTTP error creating '{truckEntry.Code}': {ex.Message}");
            }
        }

        return truckMap;
    }

    /// <summary>
    /// Executes a single status transition step.
    /// </summary>
    private async Task<ScenarioStepResult> ExecuteStepAsync(
        Step step,
        Dictionary<string, TruckContext> truckContextMap,
        CancellationToken ct = default)
    {
        var result = new ScenarioStepResult
        {
            StepNumber = step.StepNumber,
            TruckCode = step.TruckCode,
            TargetStatus = step.TargetStatus,
            ExpectedStatusCode = step.ExpectedOutcome.HttpStatusCode,
            ExpectedStatus = step.ExpectedOutcome.ResultStatus
        };

        // Look up truck context by Code
        if (!truckContextMap.TryGetValue(step.TruckCode, out var truckContext))
        {
            result.Passed = false;
            result.ActualStatusCode = 0;
            result.ErrorMessage = $"Truck with code '{step.TruckCode}' not found in setup.";
            Console.WriteLine($"  [STEP {step.StepNumber}] FAIL: {result.ErrorMessage}");
            return result;
        }

        try
        {
            // Send FULL update body with new TargetStatus
            var updateBody = new
            {
                truckContext.Code,
                truckContext.Name,
                Status = step.TargetStatus,
                truckContext.Description
            };

            var putResponse = await _httpClient.PutAsJsonAsync(
                $"api/trucks/{truckContext.Id}", updateBody, ct);

            result.ActualStatusCode = (int)putResponse.StatusCode;

            // Validate outcome
            bool passed = ValidateOutcome(
                putResponse,
                step.ExpectedOutcome,
                out var actualStatus,
                out var errorMessage);

            result.Passed = passed;
            result.ActualStatus = actualStatus;
            result.ErrorMessage = errorMessage;

            if (passed)
            {
                Console.WriteLine($"  [STEP {step.StepNumber}] PASS: {step.TruckCode} → {step.TargetStatus}");
                // Update truck context status for potential future steps
                truckContext.Status = step.TargetStatus;
            }
            else
            {
                Console.WriteLine($"  [STEP {step.StepNumber}] FAIL: {step.TruckCode} → {step.TargetStatus} — {errorMessage}");
            }

            return result;
        }
        catch (HttpRequestException ex)
        {
            result.Passed = false;
            result.ActualStatusCode = 0;
            result.ErrorMessage = $"HTTP request failed: {ex.Message}";
            Console.WriteLine($"  [STEP {step.StepNumber}] FAIL: {result.ErrorMessage}");
            return result;
        }
        catch (TaskCanceledException ex)
        {
            result.Passed = false;
            result.ActualStatusCode = 0;
            result.ErrorMessage = $"Request timed out: {ex.Message}";
            Console.WriteLine($"  [STEP {step.StepNumber}] FAIL: {result.ErrorMessage}");
            return result;
        }
    }

    /// <summary>
    /// Validates the outcome of a step against expected values.
    /// </summary>
    private static bool ValidateOutcome(
        HttpResponseMessage response,
        ExpectedOutcome expected,
        out string? actualStatus,
        out string? errorMessage)
    {
        actualStatus = null;
        errorMessage = null;

        // Check HTTP status code
        if ((int)response.StatusCode != expected.HttpStatusCode)
        {
            errorMessage = $"Expected HTTP {expected.HttpStatusCode}, got {(int)response.StatusCode}";
            return false;
        }

        // If expected ResultStatus is specified, validate it
        if (!string.IsNullOrWhiteSpace(expected.ResultStatus))
        {
            try
            {
                var content = response.Content.ReadAsStringAsync().Result;
                using var doc = JsonDocument.Parse(content);

                if (doc.RootElement.TryGetProperty("status", out var statusProp))
                {
                    actualStatus = statusProp.GetString();
                    if (actualStatus != expected.ResultStatus)
                    {
                        errorMessage = $"Expected status '{expected.ResultStatus}', got '{actualStatus}'";
                        return false;
                    }
                }
                else
                {
                    errorMessage = "Response does not contain 'status' field";
                    return false;
                }
            }
            catch (JsonException ex)
            {
                errorMessage = $"Failed to parse response body: {ex.Message}";
                return false;
            }
        }

        return true;
    }

    private static string? ExtractErrorMessage(string content)
    {
        try
        {
            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("error", out var errorProp))
                return errorProp.GetString();
        }
        catch
        {
            // Ignore parse errors
        }
        return null;
    }
}
