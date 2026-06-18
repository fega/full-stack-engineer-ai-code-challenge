using StratusFabTracker.Api.Domain;

namespace StratusFabTracker.Api.Application;

/// <summary>
/// Computes a coarse shop-throughput signal over a trailing window.
///
/// Model (assumptions are intentional and documented in PROCESS.md / the task RESOLUTION):
/// - Window: the last <see cref="WindowDays"/> calendar days, inclusive of today —
///   i.e. [today - (WindowDays - 1), today]. "today" comes from the injected clock.
/// - Completion: a spool first reaching the terminal <see cref="Station.Installed"/>
///   station. Each spool contributes at most one completion (its earliest Installed
///   event), bucketed by the UTC calendar day it occurred.
/// - Demand ("due"): spools whose <see cref="Spool.DueDate"/> falls on a given day.
/// - Rates: <c>CompletedPerDay</c> and <c>DuePerDay</c> are the per-day means over the
///   window (total / WindowDays). Both have units of spools/day measured over the same
///   window, so they are directly comparable.
/// - Keeping up: <c>CompletedPerDay &gt;= DuePerDay</c>. See remarks on
///   <see cref="GetThroughputAsync"/> for why this is sound but coarse.
/// </summary>
public sealed class ThroughputService
{
    /// <summary>
    /// Length of the trailing throughput window in days. The window includes today,
    /// so it spans [today - (WindowDays - 1), today].
    /// </summary>
    public const int WindowDays = 14;

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
        var windowStart = today.AddDays(-(WindowDays - 1));

        // A completion is a spool's FIRST arrival at the terminal Installed station.
        // Deduping per spool means a spool that (somehow) logs Installed more than
        // once is still counted as a single completion.
        var completionsByDay = spools
            .Select(s => s.StatusHistory
                .Where(h => h.Station == Station.Installed)
                .OrderBy(h => h.ChangedAt)
                .Select(h => (DateOnly?)DateOnly.FromDateTime(h.ChangedAt.UtcDateTime))
                .FirstOrDefault())
            .Where(day => day is { } d && d >= windowStart && d <= today)
            .GroupBy(day => day!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        // Demand: spools coming due on each day of the window.
        var dueByDay = spools
            .Where(s => s.DueDate >= windowStart && s.DueDate <= today)
            .GroupBy(s => s.DueDate)
            .ToDictionary(g => g.Key, g => g.Count());

        // Always emit exactly one row per day across the whole window, including days
        // with zero completions and zero due dates, so the series is gap-free.
        var daily = Enumerable.Range(0, WindowDays)
            .Select(offset =>
            {
                var day = windowStart.AddDays(offset);
                completionsByDay.TryGetValue(day, out var completed);
                dueByDay.TryGetValue(day, out var due);
                return new ThroughputDayDto(day, completed, due);
            })
            .ToList();

        var completedPerDay = daily.Average(x => x.Completed);
        var duePerDay = daily.Average(x => x.Due);

        // CompletedPerDay and DuePerDay are both spools/day over the same window, so the
        // comparison is dimensionally sound (not a unit mismatch). It is deliberately a
        // coarse signal: it ignores backlog carried in from before the window and does
        // not require that the spools completed in the window are the same ones that
        // came due in it. It answers "are we finishing work at least as fast as new work
        // is coming due, on average?" — not "is every individual spool on time?".
        var keepingUp = completedPerDay >= duePerDay;

        return new ThroughputDto(daily, completedPerDay, duePerDay, keepingUp);
    }
}

public sealed record ThroughputDayDto(DateOnly Day, int Completed, int Due);
public sealed record ThroughputDto(List<ThroughputDayDto> Daily, double CompletedPerDay, double DuePerDay, bool KeepingUp);
