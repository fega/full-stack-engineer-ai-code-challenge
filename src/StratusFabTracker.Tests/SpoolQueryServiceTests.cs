using StratusFabTracker.Api.Application;
using StratusFabTracker.Api.Domain;
using StratusFabTracker.Api.Infrastructure;
using Xunit;

namespace StratusFabTracker.Tests;

public class SpoolQueryServiceTests
{
    private static Spool Spool(string id, string number, Station station, DateOnly due) => new()
    {
        Id = id,
        PackageId = "PKG-1",
        SpoolNumber = number,
        DueDate = due,
        Bom = [],
        StatusHistory = station == Station.Detailing
            ? []
            : [new StatusEvent(station, new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), "tester")]
    };

    private static async Task<IReadOnlyList<SpoolSummaryDto>> SummariesFor(params Spool[] spools)
    {
        var repo = new InMemorySpoolRepository();
        await repo.SeedAsync(spools);
        return await new SpoolQueryService(repo).GetSummariesAsync();
    }

    [Fact]
    public async Task Summaries_carry_current_station_so_UI_can_group_by_station()
    {
        var future = new DateOnly(2999, 1, 1);
        var summaries = await SummariesFor(
            Spool("a", "A-1", Station.Weld, future),
            Spool("b", "B-1", Station.Weld, future),
            Spool("c", "C-1", Station.QC, future));

        Assert.Equal(2, summaries.Count(s => s.CurrentStation == "Weld"));
        Assert.Equal(1, summaries.Count(s => s.CurrentStation == "QC"));
    }

    [Fact]
    public async Task Summaries_are_ordered_by_spool_number()
    {
        var future = new DateOnly(2999, 1, 1);
        var summaries = await SummariesFor(
            Spool("c", "C-3", Station.Cut, future),
            Spool("a", "A-1", Station.Cut, future),
            Spool("b", "B-2", Station.Cut, future));

        Assert.Equal(new[] { "A-1", "B-2", "C-3" }, summaries.Select(s => s.SpoolNumber).ToArray());
    }

    [Fact]
    public async Task PastDue_flag_matches_dashboard_rule_ignoring_installed()
    {
        var past = new DateOnly(2000, 1, 1);
        var future = new DateOnly(2999, 1, 1);
        var summaries = await SummariesFor(
            Spool("overdue-wip", "OD-1", Station.Weld, past),
            Spool("overdue-done", "OD-2", Station.Installed, past),
            Spool("on-time", "OT-1", Station.Weld, future));

        Assert.True(Find(summaries, "OD-1").IsPastDue);
        Assert.False(Find(summaries, "OD-2").IsPastDue);   // installed = complete, not flagged
        Assert.False(Find(summaries, "OT-1").IsPastDue);
    }

    private static SpoolSummaryDto Find(IReadOnlyList<SpoolSummaryDto> summaries, string number) =>
        summaries.Single(s => s.SpoolNumber == number);
}
