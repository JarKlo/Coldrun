namespace Coldrun.Modules.Trucks.Policies;

public sealed class TruckStatusTransitionPolicy
{
    private static readonly HashSet<string> ValidStatuses =
        ["Out Of Service", "Loading", "To Job", "At Job", "Returning"];

    // Allowed transitions: key = current status, value = set of allowed next statuses
    private static readonly Dictionary<string, HashSet<string>> AllowedTransitions = new(StringComparer.Ordinal)
    {
        ["Out Of Service"] = ["Out Of Service", "Loading", "To Job", "At Job", "Returning"],
        ["Loading"]        = ["Loading", "Out Of Service", "To Job"],
        ["To Job"]         = ["Out Of Service", "To Job", "At Job"],
        ["At Job"]         = ["Out Of Service", "At Job", "Returning"],
        ["Returning"]      = ["Loading", "Out Of Service", "Returning"]
    };

    public bool CanTransition(string currentStatus, string newStatus)
    {
        if (!ValidStatuses.Contains(currentStatus))
            throw new ArgumentException($"Invalid current status '{currentStatus}'", nameof(currentStatus));
        if (!ValidStatuses.Contains(newStatus))
            throw new ArgumentException($"Invalid new status '{newStatus}'", nameof(newStatus));

        return AllowedTransitions[currentStatus].Contains(newStatus);
    }

    public string[] GetAllowedTransitions(string currentStatus)
    {
        if (!ValidStatuses.Contains(currentStatus))
            throw new ArgumentException($"Invalid current status '{currentStatus}'", nameof(currentStatus));

        return AllowedTransitions[currentStatus].OrderBy(s => s).ToArray();
    }
}
