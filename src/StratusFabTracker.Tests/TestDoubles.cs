using StratusFabTracker.Api.Application;
using StratusFabTracker.Api.Domain;

namespace StratusFabTracker.Tests;

/// <summary>
/// Deterministic, monotonic clock. Each read returns the current instant and then
/// advances by <paramref name="step"/>, mirroring a real wall clock where successive
/// status events get strictly increasing timestamps. This keeps advance tests
/// independent of how <c>Spool.CurrentStation</c> breaks ties on equal timestamps
/// (a separate, known defect tracked by the starter-bug playbook).
/// </summary>
public sealed class FakeClock : IClock
{
    private DateTimeOffset _now;
    private readonly TimeSpan _step;

    public FakeClock(DateTimeOffset now, TimeSpan? step = null)
    {
        _now = now;
        _step = step ?? TimeSpan.FromSeconds(1);
    }

    public DateTimeOffset UtcNow
    {
        get
        {
            var current = _now;
            _now += _step;
            return current;
        }
    }
}

/// <summary>
/// In-memory repository mirroring <see cref="Api.Infrastructure.InMemorySpoolRepository"/>
/// but seedable directly from test code.
/// </summary>
public sealed class FakeSpoolRepository : ISpoolRepository
{
    private readonly Dictionary<string, Spool> _spools = new();
    public int UpdateCount { get; private set; }

    public Task<List<Spool>> GetAllAsync() => Task.FromResult(_spools.Values.ToList());

    public Task<Spool?> GetByIdAsync(string id) =>
        Task.FromResult(_spools.TryGetValue(id, out var s) ? s : null);

    public Task UpdateAsync(Spool spool)
    {
        UpdateCount++;
        _spools[spool.Id] = spool;
        return Task.CompletedTask;
    }

    public Task SeedAsync(IEnumerable<Spool> spools)
    {
        foreach (var s in spools) _spools[s.Id] = s;
        return Task.CompletedTask;
    }
}

/// <summary>Builds spools for tests without repeating required-member boilerplate.</summary>
public static class SpoolBuilder
{
    public static Spool At(string id, Station station, DateTimeOffset changedAt, DateOnly? dueDate = null) =>
        new()
        {
            Id = id,
            PackageId = "PKG-1",
            SpoolNumber = id,
            DueDate = dueDate ?? new DateOnly(2030, 1, 1),
            Bom = [],
            StatusHistory = [new StatusEvent(station, changedAt, "seed")]
        };
}
