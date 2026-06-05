using StratusFabTracker.Api.Domain;

namespace StratusFabTracker.Api.Application;

public interface ISpoolRepository
{
    Task<List<Spool>> GetAllAsync();
    Task<Spool?> GetByIdAsync(string id);
    Task UpdateAsync(Spool spool);
    Task SeedAsync(IEnumerable<Spool> spools);
}

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
