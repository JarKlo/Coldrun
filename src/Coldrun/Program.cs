using Coldrun.Configuration;
using Coldrun.HealthChecks;
using Coldrun.Middleware;
using Coldrun.Modules.Trucks.Endpoints;
using Coldrun.Modules.Trucks.Policies;
using Coldrun.Modules.Trucks.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ── Configuration ──────────────────────────────────────────────
// Bind Coldrun section from appsettings.json (AOT-safe, no IOptions<T> reflection)
var coldrunOptions = new ColdrunOptions();
builder.Configuration.GetSection(ColdrunOptions.SectionName).Bind(coldrunOptions);
builder.Services.AddSingleton(coldrunOptions);

// ── Rate Limiting (configured from appsettings) ────────────────
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("truck-api", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = coldrunOptions.RateLimiting.PermitLimit,
                Window = TimeSpan.FromMinutes(coldrunOptions.RateLimiting.WindowMinutes)
            }));
});

// ── Health Checks ──────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddCheck<InMemoryStoreHealthCheck>("inmemorystore");

// ── Register Module Services ───────────────────────────────────
builder.Services.AddSingleton<InMemoryTruckStore>();
builder.Services.AddSingleton<TruckStatusTransitionPolicy>();
builder.Services.AddSingleton<TruckService>();

var app = builder.Build();

// ── Middleware Pipeline (order matters) ─────────────────────────

// 1. Correlation ID — first, so all downstream logs include it
app.UseCorrelationId();

// 2. Rate limiting
app.UseRateLimiter();

// 3. Request metrics — tracks every request
app.UseRequestMetrics();

// 4. Global exception handling — catches unhandled exceptions
app.UseGlobalExceptionHandling();

// ── Routing ────────────────────────────────────────────────────
app.UseRouting();

// ── Health Check Endpoints ─────────────────────────────────────
// Liveness: process is running
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    ResponseWriter = WriteHealthCheckResponse
}).AllowAnonymous();

// Readiness: application is ready to handle requests
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    ResponseWriter = WriteHealthCheckResponse
}).AllowAnonymous();

// ── Metrics Endpoint ───────────────────────────────────────────
app.MapGet("/metrics", () =>
{
    return Results.Text(RequestMetricsMiddleware.GetMetricsJson(), "application/json");
}).AllowAnonymous();

// ── Module Endpoints ───────────────────────────────────────────
if (coldrunOptions.Modules.Trucks.Enabled)
{
    app.MapTruckEndpoints();
}

app.Run();

// ── Health Check Response Writer ───────────────────────────────
static System.Threading.Tasks.Task WriteHealthCheckResponse(Microsoft.AspNetCore.Http.HttpContext context, Microsoft.Extensions.Diagnostics.HealthChecks.HealthReport report)
{
    context.Response.ContentType = "application/json";

    var response = new
    {
        status = report.Status.ToString(),
        correlationId = context.Items["X-Correlation-ID"] as string ?? context.TraceIdentifier,
        checks = report.Entries.Select(e => new
        {
            name = e.Key,
            status = e.Value.Status.ToString()
        })
    };

    var json = System.Text.Json.JsonSerializer.Serialize(response);
    return context.Response.WriteAsync(json);
}
