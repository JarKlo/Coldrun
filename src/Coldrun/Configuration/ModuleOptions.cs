namespace Coldrun.Configuration;

/// <summary>
/// Module-level configuration options.
/// Each ERP module (Trucks, Employees, Factories, Customers) has its own section.
/// </summary>
public sealed class ModuleOptions
{
    public TruckModuleOptions Trucks { get; set; } = new();

    // Future modules:
    // public EmployeeModuleOptions Employees { get; set; } = new();
    // public FactoryModuleOptions Factories { get; set; } = new();
    // public CustomerModuleOptions Customers { get; set; } = new();
}

/// <summary>
/// Truck module-specific configuration.
/// </summary>
public sealed class TruckModuleOptions
{
    /// <summary>
    /// Whether the Truck module is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;
}
