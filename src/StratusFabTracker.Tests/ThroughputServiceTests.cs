using StratusFabTracker.Api.Application;
using StratusFabTracker.Api.Domain;
using StratusFabTracker.Api.Infrastructure;
using Xunit;

namespace StratusFabTracker.Tests;

/// <summary>
/// Pins the throughput model with a fixed clock and hand-constructed history.
/// "Today" is frozen to 2026-06-18, so the 14-day window is [2026-06-05, 2026-06-18].
/// </summary>
public class ThroughputServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 18, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 6, 18);
    private static readonly DateOnly WindowStart = new(2026, 6, 5); // Today - (WindowDays - 1)

    // A due date far in the future, used when a spool should not contribute to demand.
    private static readonly DateOnly NotDue = new(2999, 1, 1);

    private static DateTimeOffset At(DateOnly day) =>
        new(day.Year, day.Month, day.Day, 12, 0, 0, TimeSpan.Zero);

    private static Spool Spool(string id, DateOnly due, params DateTimeOffset[] installedAt) => new()
    {
        Id = id,
        PackageId = "PKG-1",
        SpoolNumber = id,
        DueDate = due,
        Bom = [],
        StatusHistory = installedAt.Select(t => new StatusEvent(Station.Installed, t, "seed")).ToList()
    };

    private static async Task<ThroughputDto> ThroughputFor(params Spool[] spools)
    {
        var repo = new InMemorySpoolRepository();
        await repo.SeedAsync(spools);
        // Step zero: the service reads the clock once, and a frozen instant keeps "today" stable.
        var service = new ThroughputService(repo, new FakeClock(Now, TimeSpan.Zero));
        return await service.GetThroughputAsync();
    }

    private static int CompletedOn(ThroughputDto dto, DateOnly day) =>
        dto.Daily.Single(d => d.Day == day).Completed;

    private static int DueOn(ThroughputDto dto, DateOnly day) =>
        dto.Daily.Single(d => d.Day == day).Due;

    [Fact]
    public async Task Daily_series_covers_the_full_window_including_zero_days()
    {
        var dto = await ThroughputFor(); // no spools at all

        Assert.Equal(ThroughputService.WindowDays, dto.Daily.Count);
        Assert.Equal(WindowStart, dto.Daily.First().Day);
        Assert.Equal(Today, dto.Daily.Last().Day);

        // Days are consecutive with no gaps.
        for (var i = 0; i < dto.Daily.Count; i++)
            Assert.Equal(WindowStart.AddDays(i), dto.Daily[i].Day);

        Assert.All(dto.Daily, d =>
        {
            Assert.Equal(0, d.Completed);
            Assert.Equal(0, d.Due);
        });
        Assert.Equal(0, dto.CompletedPerDay);
        Assert.Equal(0, dto.DuePerDay);
        Assert.True(dto.KeepingUp); // 0 >= 0
    }

    [Fact]
    public async Task Completions_bucket_by_day_and_average_over_the_full_window()
    {
        var dto = await ThroughputFor(
            Spool("a", NotDue, At(new DateOnly(2026, 6, 10))),
            Spool("b", NotDue, At(new DateOnly(2026, 6, 10))),
            Spool("c", NotDue, At(Today)),
            Spool("old", NotDue, At(new DateOnly(2026, 6, 4)))); // one day before the window

        Assert.Equal(2, CompletedOn(dto, new DateOnly(2026, 6, 10)));
        Assert.Equal(1, CompletedOn(dto, Today));
        Assert.Equal(3, dto.Daily.Sum(d => d.Completed)); // "old" excluded
        // Average is total / WindowDays, not total / busy-days.
        Assert.Equal(3 / 14.0, dto.CompletedPerDay, 10);
    }

    [Fact]
    public async Task Each_spool_counts_once_at_its_first_installed_event()
    {
        // Two Installed events recorded out of order; only the earliest (06-08) counts.
        var dto = await ThroughputFor(
            Spool("dup", NotDue, At(new DateOnly(2026, 6, 12)), At(new DateOnly(2026, 6, 8))));

        Assert.Equal(1, CompletedOn(dto, new DateOnly(2026, 6, 8)));
        Assert.Equal(0, CompletedOn(dto, new DateOnly(2026, 6, 12)));
        Assert.Equal(1, dto.Daily.Sum(d => d.Completed));
    }

    [Fact]
    public async Task Window_boundaries_are_inclusive_of_start_and_today()
    {
        var dto = await ThroughputFor(
            Spool("edge-in", NotDue, At(WindowStart)),
            Spool("edge-out", NotDue, At(WindowStart.AddDays(-1))));

        Assert.Equal(1, CompletedOn(dto, WindowStart));
        Assert.Equal(1, dto.Daily.Sum(d => d.Completed)); // the day-before completion is excluded
    }

    [Fact]
    public async Task Due_dates_bucket_by_day_and_average_over_the_full_window()
    {
        var dto = await ThroughputFor(
            Spool("d1", new DateOnly(2026, 6, 7)),
            Spool("d2", new DateOnly(2026, 6, 7)),
            Spool("d3", Today),
            Spool("after", new DateOnly(2026, 6, 19)),   // tomorrow: outside window
            Spool("before", new DateOnly(2026, 6, 4)));  // day before window start

        Assert.Equal(2, DueOn(dto, new DateOnly(2026, 6, 7)));
        Assert.Equal(1, DueOn(dto, Today));
        Assert.Equal(3, dto.Daily.Sum(d => d.Due)); // before/after excluded
        Assert.Equal(3 / 14.0, dto.DuePerDay, 10);
    }

    [Fact]
    public async Task KeepingUp_is_true_when_completion_rate_meets_or_exceeds_due_rate()
    {
        var dto = await ThroughputFor(
            Spool("c1", NotDue, At(Today)),
            Spool("c2", NotDue, At(new DateOnly(2026, 6, 10))),
            Spool("due1", new DateOnly(2026, 6, 9))); // 2 completed vs 1 due

        Assert.Equal(2 / 14.0, dto.CompletedPerDay, 10);
        Assert.Equal(1 / 14.0, dto.DuePerDay, 10);
        Assert.True(dto.KeepingUp);
    }

    [Fact]
    public async Task KeepingUp_is_false_when_demand_outpaces_completions()
    {
        var dto = await ThroughputFor(
            Spool("c1", NotDue, At(Today)),
            Spool("due1", new DateOnly(2026, 6, 9)),
            Spool("due2", new DateOnly(2026, 6, 10)),
            Spool("due3", new DateOnly(2026, 6, 11))); // 1 completed vs 3 due

        Assert.False(dto.KeepingUp);
    }
}
