using Coldrun.Modules.Trucks.Policies;

namespace Coldrun.Modules.Trucks.Models;

public sealed class Truck
{
    public Guid Id { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public string Status { get; private set; }
    public string? Description { get; private set; }

    private Truck()
    {
        Code = string.Empty;
        Name = string.Empty;
        Status = string.Empty;
    }

    public static Truck Create(string code, string name, string status, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required", nameof(code));
        if (!IsAlphanumeric(code))
            throw new ArgumentException("Code must be alphanumeric", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required", nameof(name));
        if (string.IsNullOrWhiteSpace(status))
            throw new ArgumentException("Status is required", nameof(status));
        if (!IsValidStatus(status))
            throw new ArgumentException($"Invalid status '{status}'. Must be one of: {string.Join(", ", ValidStatuses)}", nameof(status));

        return new Truck
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            Status = status,
            Description = description
        };
    }

    public void Update(string code, string name, string status, TruckStatusTransitionPolicy transitionPolicy, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required", nameof(code));
        if (!IsAlphanumeric(code))
            throw new ArgumentException("Code must be alphanumeric", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required", nameof(name));
        if (string.IsNullOrWhiteSpace(status))
            throw new ArgumentException("Status is required", nameof(status));
        if (!IsValidStatus(status))
            throw new ArgumentException($"Invalid status '{status}'. Must be one of: {string.Join(", ", ValidStatuses)}", nameof(status));

        if (status != Status && !transitionPolicy.CanTransition(Status, status))
            throw new InvalidOperationException(
                $"Invalid status transition from '{Status}' to '{status}'. " +
                $"Allowed transitions from '{Status}': {string.Join(", ", transitionPolicy.GetAllowedTransitions(Status))}");

        Code = code;
        Name = name;
        Status = status;
        Description = description;
    }

    private static bool IsAlphanumeric(string value) =>
        value.All(c => (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'));

    private static bool IsValidStatus(string status) =>
        ValidStatuses.Contains(status, StringComparer.Ordinal);

    public static string[] ValidStatuses =>
        ["Out Of Service", "Loading", "To Job", "At Job", "Returning"];
}
