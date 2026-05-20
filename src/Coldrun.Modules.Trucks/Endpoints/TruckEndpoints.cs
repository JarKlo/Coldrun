using Coldrun.Modules.Trucks.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Coldrun.Modules.Trucks.Endpoints;

public static class TruckEndpoints
{
    public static WebApplication MapTruckEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/trucks")
            .RequireRateLimiting("truck-api");

        group.MapPost("/", CreateTruck);
        group.MapGet("/{id:guid}", GetTruck);
        group.MapGet("/", ListTrucks);
        group.MapPut("/{id:guid}", UpdateTruck);
        group.MapDelete("/{id:guid}", DeleteTruck);

        return app;
    }

    private static IResult CreateTruck(TruckCreateRequest request, TruckService service)
    {
        var truck = service.Create(request.Code, request.Name, request.Status, request.Description);
        return Results.Created($"/api/trucks/{truck.Id}", TruckDto.FromEntity(truck));
    }

    private static IResult GetTruck(Guid id, TruckService service)
    {
        var truck = service.GetById(id);
        return truck is not null
            ? Results.Ok(TruckDto.FromEntity(truck))
            : Results.NotFound(new { error = $"Truck with id '{id}' not found" });
    }

    private static IResult ListTrucks([AsParameters] TruckListRequest request, TruckService service)
    {
        var trucks = service.List(request.Code, request.Name, request.Status, request.SortBy, request.SortDir);
        return Results.Ok(trucks);
    }

    private static IResult UpdateTruck(Guid id, TruckUpdateRequest request, TruckService service)
    {
        var truck = service.Update(id, request.Code, request.Name, request.Status, request.Description);
        return Results.Ok(TruckDto.FromEntity(truck));
    }

    private static IResult DeleteTruck(Guid id, TruckService service)
    {
        var deleted = service.Delete(id);
        return deleted
            ? Results.NoContent()
            : Results.NotFound(new { error = $"Truck with id '{id}' not found" });
    }
}

public sealed record TruckCreateRequest(
    string Code,
    string Name,
    string Status,
    string? Description = null);

public sealed record TruckUpdateRequest(
    string Code,
    string Name,
    string Status,
    string? Description = null);

public sealed record TruckListRequest(
    string? Code = null,
    string? Name = null,
    string? Status = null,
    string SortBy = "code",
    string SortDir = "asc");
