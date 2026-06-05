using StratusFabTracker.Api.Application;
using StratusFabTracker.Api.Domain;

namespace StratusFabTracker.Api.Infrastructure;

public sealed class InMemorySpoolRepository : ISpoolRepository
{
    private readonly Dictionary<string, Spool> _spools = new();

    public Task<List<Spool>> GetAllAsync() => Task.FromResult(_spools.Values.ToList());
    public Task<Spool?> GetByIdAsync(string id) => Task.FromResult(_spools.TryGetValue(id, out var spool) ? spool : null);

    public Task UpdateAsync(Spool spool)
    {
        _spools[spool.Id] = spool;
        return Task.CompletedTask;
    }

    public Task SeedAsync(IEnumerable<Spool> spools)
    {
        foreach (var spool in spools) _spools[spool.Id] = spool;
        return Task.CompletedTask;
    }
}
