using Coldrun.Modules.Trucks.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Coldrun.Modules.Trucks.Endpoints;

/// <summary>
/// Maps the Truck module REST API endpoints.
/// </summary>
public static class TruckEndpoints
{
    /// <summary>
    /// Maps all Truck endpoints under <c>/api/trucks</c> with rate limiting.
    /// </summary>
    public static WebApplication MapTruckEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/trucks")
            .WithTags("Trucks")
            .RequireRateLimiting("truck-api");

        group.MapPost("/", CreateTruck)
            .WithName("CreateTruck")
            .WithSummary("Create a new truck")
            .WithDescription("Creates a truck with the specified code, name, status, and optional description. Code must be unique (case-insensitive) and alphanumeric. Status must be one of: Out Of Service, Loading, To Job, At Job, Returning.")
            .Produces<TruckDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/{id:guid}", GetTruck)
            .WithName("GetTruck")
            .WithSummary("Get a truck by ID")
            .WithDescription("Retrieves a single truck by its unique identifier.")
            .Produces<TruckDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/", ListTrucks)
            .WithName("ListTrucks")
            .WithSummary("List trucks with optional filtering and sorting")
            .WithDescription("Retrieves a list of trucks. Supports filtering by code, name, and status, and sorting by code, name, or status in ascending or descending order.")
            .Produces<List<TruckDto>>();

        group.MapPut("/{id:guid}", UpdateTruck)
            .WithName("UpdateTruck")
            .WithSummary("Update an existing truck")
            .WithDescription("Updates a truck's code, name, status, and optional description. Status transitions are validated against the finite state machine rules. Code must remain unique (case-insensitive).")
            .Produces<TruckDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/{id:guid}", DeleteTruck)
            .WithName("DeleteTruck")
            .WithSummary("Delete a truck")
            .WithDescription("Permanently deletes a truck by its unique identifier.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

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

/// <summary>
/// Request payload for creating a new truck.
/// </summary>
/// <param name="Code">Unique alphanumeric code (case-insensitive). Required.</param>
/// <param name="Name">Display name of the truck. Required.</param>
/// <param name="Status">Initial status. Must be one of: Out Of Service, Loading, To Job, At Job, Returning. Required.</param>
/// <param name="Description">Optional description of the truck.</param>
public sealed record TruckCreateRequest(
    string Code,
    string Name,
    string Status,
    string? Description = null);

/// <summary>
/// Request payload for updating an existing truck.
/// </summary>
/// <param name="Code">Unique alphanumeric code (case-insensitive). Required.</param>
/// <param name="Name">Display name of the truck. Required.</param>
/// <param name="Status">New status. Must be one of: Out Of Service, Loading, To Job, At Job, Returning. Required.</param>
/// <param name="Description">Optional description of the truck.</param>
public sealed record TruckUpdateRequest(
    string Code,
    string Name,
    string Status,
    string? Description = null);

/// <summary>
/// Query parameters for listing trucks with filtering and sorting.
/// </summary>
/// <param name="Code">Filter by code (partial match, case-insensitive).</param>
/// <param name="Name">Filter by name (partial match, case-insensitive).</param>
/// <param name="Status">Filter by exact status value.</param>
/// <param name="SortBy">Sort field: code, name, or status. Default: code.</param>
/// <param name="SortDir">Sort direction: asc or desc. Default: asc.</param>
public sealed record TruckListRequest(
    string? Code = null,
    string? Name = null,
    string? Status = null,
    string SortBy = "code",
    string SortDir = "asc");
