namespace Coldrun.Seeder.Models;

/// <summary>
/// Root JSON structure for seed data files.
/// </summary>
public sealed class TruckSeedFile
{
    public List<TruckSeedEntry> Trucks { get; set; } = [];
}

/// <summary>
/// Individual truck entry in a seed data file.
/// Maps to the API's TruckCreateRequest (Code, Name, Status, Description).
/// </summary>
public sealed class TruckSeedEntry
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Status { get; set; } = "";
    public string? Description { get; set; }
}
