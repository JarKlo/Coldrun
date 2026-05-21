using System.Net.Http.Json;
using System.Text.Json;
using Coldrun.Seeder.Models;

namespace Coldrun.Seeder.Services;

/// <summary>
/// HttpClient-based seeder that calls the Truck API via HTTP.
/// </summary>
public sealed class ApiSeeder(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    /// <summary>
    /// Verifies the API is running by hitting the health check endpoint.
    /// </summary>
    public async Task<bool> HealthCheckAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("health/live", ct);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"[ERROR] HTTP request failed during health check: {ex.Message}");
            return false;
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine("[ERROR] Health check timed out. API may not be running.");
            return false;
        }
    }

    /// <summary>
    /// Deletes all existing trucks from the API.
    /// </summary>
    public async Task ResetAsync(CancellationToken ct = default)
    {
        try
        {
            var trucks = await _httpClient.GetFromJsonAsync<List<TruckDto>>("api/trucks", ct);
            if (trucks is null || trucks.Count == 0)
            {
                Console.WriteLine("  No existing trucks to delete.");
                return;
            }

            foreach (var truck in trucks)
            {
                var response = await _httpClient.DeleteAsync($"api/trucks/{truck.Id}", ct);
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"  Deleted: {truck.Code} ({truck.Id})");
                }
                else
                {
                    Console.WriteLine($"  Failed to delete {truck.Code}: {response.StatusCode}");
                }
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"[ERROR] HTTP request failed during reset: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Seeds trucks from a JSON file into the API.
    /// </summary>
    public async Task<SeedResult> SeedAsync(string jsonFilePath, CancellationToken ct = default)
    {
        if (!File.Exists(jsonFilePath))
            throw new FileNotFoundException($"JSON file not found: {jsonFilePath}");

        var json = await File.ReadAllTextAsync(jsonFilePath, ct);
        TruckSeedFile? data;
        try
        {
            data = JsonSerializer.Deserialize<TruckSeedFile>(json);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to parse JSON file '{jsonFilePath}': {ex.Message}", ex);
        }

        if (data is null || data.Trucks.Count == 0)
            throw new InvalidOperationException("JSON file contains no truck data.");

        var result = new SeedResult();

        foreach (var entry in data.Trucks)
        {
            ct.ThrowIfCancellationRequested();

            // Check if truck already exists
            bool exists;
            try
            {
                exists = await CheckExistsAsync(entry.Code, ct);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"[ERROR] Failed to check existence for '{entry.Code}': {ex.Message}");
                result.Failed++;
                continue;
            }

            if (exists)
            {
                Console.WriteLine($"  Skipped: {entry.Code} (already exists)");
                result.Skipped++;
                continue;
            }

            // Create truck
            try
            {
                var createResponse = await _httpClient.PostAsJsonAsync("api/trucks",
                    new { entry.Code, entry.Name, entry.Status, entry.Description }, ct);

                if (createResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine($"  Created: {entry.Code}");
                    result.Created++;
                }
                else
                {
                    var errorContent = await createResponse.Content.ReadAsStringAsync(ct);
                    var errorMessage = ExtractErrorMessage(errorContent) ?? createResponse.StatusCode.ToString();
                    Console.WriteLine($"  Failed: {entry.Code} — {errorMessage}");
                    result.Failed++;
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"[ERROR] Failed to create '{entry.Code}': {ex.Message}");
                result.Failed++;
            }
        }

        return result;
    }

    private async Task<bool> CheckExistsAsync(string code, CancellationToken ct)
    {
        var response = await _httpClient.GetFromJsonAsync<List<TruckDto>>(
            $"api/trucks?code={Uri.EscapeDataString(code)}", ct);
        return response is not null && response.Count > 0;
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

/// <summary>
/// Minimal DTO for deserializing truck data from API responses.
/// </summary>
public sealed record TruckDto(Guid Id, string Code, string Name, string Status, string? Description);
