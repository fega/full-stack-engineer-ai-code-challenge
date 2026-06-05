using StratusFabTracker.Api.Domain;

namespace StratusFabTracker.Api.Application;

public sealed class ThroughputService
{
    private readonly ISpoolRepository _repository;
    private readonly IClock _clock;

    public ThroughputService(ISpoolRepository repository, IClock clock)
    {
        _repository = repository;
        _clock = clock;
    }

    public async Task<ThroughputDto> GetThroughputAsync()
    {
        var spools = await _repository.GetAllAsync();
        var today = DateOnly.FromDateTime(_clock.UtcNow.UtcDateTime);
        var windowStart = today.AddDays(-13);

        var completions = spools
            .SelectMany(s => s.StatusHistory.Where(h => h.Station == Station.Installed)
                .Select(h => new { SpoolId = s.Id, Day = DateOnly.FromDateTime(h.ChangedAt.UtcDateTime) }))
            .Where(x => x.Day >= windowStart && x.Day <= today)
            .GroupBy(x => x.Day)
            .ToDictionary(g => g.Key, g => g.Count());

        var daily = Enumerable.Range(0, 14)
            .Select(offset =>
            {
                var day = windowStart.AddDays(offset);
                completions.TryGetValue(day, out var count);
                return new ThroughputDayDto(day, count);
            })
            .ToList();

        var completedPerDay = daily.Average(x => x.Completed);
        var duePerDay = spools.Count(x => x.DueDate >= windowStart && x.DueDate <= today) / 14.0;

        return new ThroughputDto(daily, completedPerDay, duePerDay, completedPerDay >= duePerDay);
    }
}

public sealed record ThroughputDayDto(DateOnly Day, int Completed);
public sealed record ThroughputDto(List<ThroughputDayDto> Daily, double CompletedPerDay, double DuePerDay, bool KeepingUp);
