using StratusFabTracker.Api.Application;
using StratusFabTracker.Api.Domain;
using Xunit;

namespace StratusFabTracker.Tests;

public class SpoolWorkflowServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 18, 12, 0, 0, TimeSpan.Zero);

    private static (SpoolWorkflowService service, FakeSpoolRepository repo) BuildWith(params Spool[] spools)
    {
        var repo = new FakeSpoolRepository();
        repo.SeedAsync(spools).GetAwaiter().GetResult();
        return (new SpoolWorkflowService(repo, new FakeClock(Now)), repo);
    }

    [Fact]
    public async Task Advance_moves_spool_one_station_forward()
    {
        var (service, repo) = BuildWith(SpoolBuilder.At("S1", Station.Detailing, Now.AddDays(-1)));

        var outcome = await service.AdvanceAsync("S1");

        Assert.Equal(TransitionResult.Success, outcome.Result);
        Assert.Equal(Station.Cut, outcome.NewStation);

        var spool = await repo.GetByIdAsync("S1");
        Assert.Equal(Station.Cut, spool!.CurrentStation);
    }

    [Fact]
    public async Task Advance_records_a_status_event_with_clock_time_and_actor()
    {
        var (service, repo) = BuildWith(SpoolBuilder.At("S1", Station.Cut, Now.AddDays(-1)));

        await service.AdvanceAsync("S1");

        var spool = await repo.GetByIdAsync("S1");
        // Assert via the public surface clients use, not by re-deriving "latest".
        Assert.Equal(Station.Weld, spool!.CurrentStation);
        Assert.Equal(2, spool.StatusHistory.Count); // original + appended, history is preserved

        // The appended event carries the clock time and the system actor.
        var appended = Assert.Single(spool.StatusHistory, e => e.Station == Station.Weld);
        Assert.Equal(Now, appended.ChangedAt);
        Assert.Equal("system", appended.ChangedBy);
    }

    [Theory]
    [InlineData(Station.Detailing, Station.Cut)]
    [InlineData(Station.Cut, Station.Weld)]
    [InlineData(Station.Weld, Station.QC)]
    [InlineData(Station.QC, Station.Shipped)]
    [InlineData(Station.Shipped, Station.Installed)]
    public async Task Advance_follows_every_forward_hop_in_the_chain(Station from, Station expected)
    {
        var (service, repo) = BuildWith(SpoolBuilder.At("S1", from, Now.AddDays(-1)));

        var outcome = await service.AdvanceAsync("S1");

        Assert.Equal(TransitionResult.Success, outcome.Result);
        Assert.Equal(expected, outcome.NewStation);
        Assert.Equal(expected, (await repo.GetByIdAsync("S1"))!.CurrentStation);
    }

    [Fact]
    public async Task Advance_walks_a_spool_through_the_whole_chain()
    {
        var (service, repo) = BuildWith(SpoolBuilder.At("S1", Station.Detailing, Now.AddDays(-1)));
        var expected = new[] { Station.Cut, Station.Weld, Station.QC, Station.Shipped, Station.Installed };

        foreach (var station in expected)
        {
            var outcome = await service.AdvanceAsync("S1");
            Assert.Equal(TransitionResult.Success, outcome.Result);
            Assert.Equal(station, outcome.NewStation);
        }

        Assert.Equal(Station.Installed, (await repo.GetByIdAsync("S1"))!.CurrentStation);
    }

    [Fact]
    public async Task Advance_unknown_spool_returns_NotFound()
    {
        var (service, _) = BuildWith();

        var outcome = await service.AdvanceAsync("does-not-exist");

        Assert.Equal(TransitionResult.NotFound, outcome.Result);
        Assert.Null(outcome.NewStation);
    }

    [Fact]
    public async Task Advance_at_Installed_is_rejected_and_does_not_mutate_state()
    {
        var (service, repo) = BuildWith(SpoolBuilder.At("S1", Station.Installed, Now.AddDays(-1)));

        var outcome = await service.AdvanceAsync("S1");

        Assert.Equal(TransitionResult.InvalidTransition, outcome.Result);
        Assert.Null(outcome.NewStation);

        var spool = await repo.GetByIdAsync("S1");
        Assert.Single(spool!.StatusHistory);          // no event appended
        Assert.Equal(0, repo.UpdateCount);            // repository never written
        Assert.Equal(Station.Installed, spool.CurrentStation);
    }
}
