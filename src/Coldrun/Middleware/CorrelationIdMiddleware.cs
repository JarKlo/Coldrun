using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Coldrun.Middleware;

/// <summary>
/// Middleware that generates or propagates a correlation ID for each request.
/// The correlation ID is added to the request/response headers and logged with every log entry.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    private const string CorrelationIdHeaderName = "X-Correlation-ID";
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Generate or propagate correlation ID
        var correlationId = context.Request.Headers[CorrelationIdHeaderName].ToString();
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
        }

        // Store for downstream use
        context.Items[CorrelationIdHeaderName] = correlationId;

        // Log request start
        _logger.LogInformation(
            "Request started: {Method} {Path} | CorrelationId: {CorrelationId}",
            context.Request.Method,
            context.Request.Path,
            correlationId);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            // Always set response header, but only if response hasn't started yet
            if (!context.Response.HasStarted)
            {
                context.Response.Headers[CorrelationIdHeaderName] = correlationId;
            }

            // Log request end
            _logger.LogInformation(
                "Request completed: {Method} {Path} | Status: {StatusCode} | Duration: {ElapsedMs}ms | CorrelationId: {CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                correlationId);
        }
    }
}

/// <summary>
/// Extension method for adding CorrelationIdMiddleware to the pipeline.
/// </summary>
public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CorrelationIdMiddleware>();
    }
}
