using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace Coldrun.Middleware;

/// <summary>
/// Global exception handling middleware.
/// Catches unhandled exceptions, logs them with correlation ID, and returns a consistent error response.
/// Replaces per-endpoint try/catch blocks for cleaner endpoint code.
/// </summary>
public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Get correlation ID
        var correlationId = context.Items["X-Correlation-ID"] as string ?? context.TraceIdentifier;

        // Determine HTTP status code and error message based on exception type
        (HttpStatusCode statusCode, string message) = exception switch
        {
            ArgumentException argEx => (HttpStatusCode.BadRequest, argEx.Message),
            InvalidOperationException opEx when opEx.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase)
                => (HttpStatusCode.Conflict, opEx.Message),
            InvalidOperationException opEx when opEx.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)
                => (HttpStatusCode.NotFound, opEx.Message),
            InvalidOperationException opEx => (HttpStatusCode.BadRequest, opEx.Message),
            _ => (HttpStatusCode.InternalServerError, _environment.IsDevelopment()
                ? $"{exception.GetType().Name}: {exception.Message}"
                : "An unexpected error occurred. Please try again later.")
        };

        // Log the exception with correlation ID
        _logger.LogError(
            exception,
            "Unhandled exception: {Method} {Path} | Status: {StatusCode} | Exception: {ExceptionType} | CorrelationId: {CorrelationId}",
            context.Request.Method,
            context.Request.Path,
            (int)statusCode,
            exception.GetType().Name,
            correlationId);

        // Set response
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var errorResponse = new { error = message };
        var json = JsonSerializer.Serialize(errorResponse);
        await context.Response.WriteAsync(json);
    }
}

/// <summary>
/// Extension method for adding GlobalExceptionMiddleware to the pipeline.
/// </summary>
public static class GlobalExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionMiddleware>();
    }
}
