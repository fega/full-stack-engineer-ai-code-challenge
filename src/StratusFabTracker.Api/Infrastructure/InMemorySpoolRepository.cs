using System.Collections.Concurrent;
using StratusFabTracker.Api.Application;
using StratusFabTracker.Api.Domain;

namespace StratusFabTracker.Api.Infrastructure;

public sealed class InMemorySpoolRepository : ISpoolRepository
{
    // Concurrent: this repository is a singleton serving concurrent requests, and
    // the advance endpoint mutates entries while reads enumerate them.
    private readonly ConcurrentDictionary<string, Spool> _spools = new();

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
