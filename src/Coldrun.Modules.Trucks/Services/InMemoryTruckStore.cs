using Coldrun.Modules.Trucks.Models;

namespace Coldrun.Modules.Trucks.Services;

public sealed class InMemoryTruckStore
{
    private readonly List<Truck> _trucks = [];
    private readonly ReaderWriterLockSlim _lock = new();

    public Truck? GetById(Guid id)
    {
        _lock.EnterReadLock();
        try
        {
            return _trucks.FirstOrDefault(t => t.Id == id);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Truck? GetByCode(string code)
    {
        _lock.EnterReadLock();
        try
        {
            return _trucks.FirstOrDefault(t => string.Equals(t.Code, code, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public bool ExistsByCode(string code, Guid? excludeId = null)
    {
        _lock.EnterReadLock();
        try
        {
            return _trucks.Any(t => string.Equals(t.Code, code, StringComparison.OrdinalIgnoreCase) && (!excludeId.HasValue || t.Id != excludeId.Value));
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public List<Truck> GetAll()
    {
        _lock.EnterReadLock();
        try
        {
            return [.. _trucks];
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Atomically checks for code uniqueness and adds the truck under a single write lock.
    /// Eliminates the check-then-act race condition in concurrent create scenarios.
    /// </summary>
    public bool TryAdd(Truck truck, out string? conflictReason)
    {
        _lock.EnterWriteLock();
        try
        {
            if (_trucks.Any(t => string.Equals(t.Code, truck.Code, StringComparison.OrdinalIgnoreCase)))
            {
                conflictReason = $"Truck with code '{truck.Code}' already exists";
                return false;
            }
            _trucks.Add(truck);
            conflictReason = null;
            return true;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void Update(Truck truck)
    {
        _lock.EnterWriteLock();
        try
        {
            var index = _trucks.FindIndex(t => t.Id == truck.Id);
            if (index >= 0)
                _trucks[index] = truck;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public bool Delete(Guid id)
    {
        _lock.EnterWriteLock();
        try
        {
            return _trucks.RemoveAll(t => t.Id == id) > 0;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
}
