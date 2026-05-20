using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Coldrun.Middleware;

/// <summary>
/// Middleware that tracks basic request metrics using atomic counters.
/// Exposes metrics via /metrics endpoint in JSON format.
/// AOT-compatible — no reflection, no external dependencies.
/// </summary>
public sealed class RequestMetricsMiddleware
{
    /// <summary>
    /// Wrapper class for atomic counter operations.
    /// Required because ConcurrentDictionary doesn't support ref returns for Interlocked.Increment.
    /// </summary>
    private sealed class AtomicCounter
    {
        public long Value;
    }

    private static long _totalRequests;
    private static readonly ConcurrentDictionary<string, AtomicCounter> _requestsByEndpoint = new();
    private static readonly ConcurrentDictionary<string, AtomicCounter> _requestsByStatusCategory = new();

    private readonly RequestDelegate _next;
    private readonly ILogger<RequestMetricsMiddleware> _logger;

    public RequestMetricsMiddleware(RequestDelegate next, ILogger<RequestMetricsMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        Interlocked.Increment(ref _totalRequests);

        // Extract route pattern from endpoint (dynamic, module-agnostic)
        var endpoint = context.GetEndpoint();
        var routePattern = endpoint?.DisplayName ?? "Unknown";

        await _next(context);

        // Categorize status code
        var statusCategory = context.Response.StatusCode switch
        {
            >= 200 and < 300 => "Success",
            >= 300 and < 400 => "Redirect",
            >= 400 and < 500 => "ClientError",
            >= 500 => "ServerError",
            _ => "Unknown"
        };

        IncrementCounter(_requestsByEndpoint, routePattern);
        IncrementCounter(_requestsByStatusCategory, statusCategory);
    }

    private static void IncrementCounter(ConcurrentDictionary<string, AtomicCounter> dictionary, string key)
    {
        var counter = dictionary.GetOrAdd(key, static _ => new AtomicCounter());
        Interlocked.Increment(ref counter.Value);
    }

    /// <summary>
    /// Returns current metrics as a JSON string.
    /// </summary>
    public static string GetMetricsJson()
    {
        var metrics = new
        {
            totalRequests = Interlocked.Read(ref _totalRequests),
            requestsByEndpoint = _requestsByEndpoint.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Value),
            requestsByStatusCategory = _requestsByStatusCategory.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Value)
        };

        return JsonSerializer.Serialize(metrics);
    }

    /// <summary>
    /// Resets all metrics counters. Useful for testing.
    /// </summary>
    public static void Reset()
    {
        _totalRequests = 0;
        _requestsByEndpoint.Clear();
        _requestsByStatusCategory.Clear();
    }
}

/// <summary>
/// Extension method for adding RequestMetricsMiddleware to the pipeline.
/// </summary>
public static class RequestMetricsMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestMetrics(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestMetricsMiddleware>();
    }
}
