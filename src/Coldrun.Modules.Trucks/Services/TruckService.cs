using Coldrun.Modules.Trucks.Models;
using Coldrun.Modules.Trucks.Policies;

namespace Coldrun.Modules.Trucks.Services;

public sealed class TruckService
{
    private readonly InMemoryTruckStore _store;
    private readonly TruckStatusTransitionPolicy _transitionPolicy;

    public TruckService(InMemoryTruckStore store, TruckStatusTransitionPolicy transitionPolicy)
    {
        _store = store;
        _transitionPolicy = transitionPolicy;
    }

    public Truck Create(string code, string name, string status, string? description = null)
    {
        var truck = Truck.Create(code, name, status, description);
        if (!_store.TryAdd(truck, out var conflictReason))
            throw new InvalidOperationException(conflictReason!);
        return truck;
    }

    public Truck? GetById(Guid id) => _store.GetById(id);

    public List<TruckDto> List(string? code = null, string? name = null, string? status = null, string sortBy = "code", string sortDir = "asc")
    {
        var trucks = _store.GetAll();

        // Filter
        if (!string.IsNullOrWhiteSpace(code))
            trucks = trucks.Where(t => t.Code.Contains(code, StringComparison.OrdinalIgnoreCase)).ToList();
        if (!string.IsNullOrWhiteSpace(name))
            trucks = trucks.Where(t => t.Name.Contains(name, StringComparison.OrdinalIgnoreCase)).ToList();
        if (!string.IsNullOrWhiteSpace(status))
            trucks = trucks.Where(t => t.Status.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();

        // Sort
        trucks = sortBy.ToLowerInvariant() switch
        {
            "code" => sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase)
                ? trucks.OrderByDescending(t => t.Code).ToList()
                : trucks.OrderBy(t => t.Code).ToList(),
            "name" => sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase)
                ? trucks.OrderByDescending(t => t.Name).ToList()
                : trucks.OrderBy(t => t.Name).ToList(),
            "status" => sortDir.Equals("desc", StringComparison.OrdinalIgnoreCase)
                ? trucks.OrderByDescending(t => t.Status).ToList()
                : trucks.OrderBy(t => t.Status).ToList(),
            _ => trucks.OrderBy(t => t.Code).ToList()
        };

        return trucks.Select(TruckDto.FromEntity).ToList();
    }

    public Truck Update(Guid id, string code, string name, string status, string? description = null)
    {
        var truck = _store.GetById(id)
            ?? throw new InvalidOperationException($"Truck with id '{id}' not found");

        if (_store.ExistsByCode(code, id))
            throw new InvalidOperationException($"Truck with code '{code}' already exists");

        truck.Update(code, name, status, _transitionPolicy, description);
        _store.Update(truck);
        return truck;
    }

    public bool Delete(Guid id) => _store.Delete(id);
}

public sealed record TruckDto(Guid Id, string Code, string Name, string Status, string? Description)
{
    public static TruckDto FromEntity(Truck truck) =>
        new(truck.Id, truck.Code, truck.Name, truck.Status, truck.Description);
}
