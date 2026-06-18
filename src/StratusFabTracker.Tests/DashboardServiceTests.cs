using StratusFabTracker.Api.Application;
using StratusFabTracker.Api.Domain;
using StratusFabTracker.Api.Infrastructure;
using Xunit;

namespace StratusFabTracker.Tests;

public class DashboardServiceTests
{
    private static readonly DateOnly Future = new(2999, 1, 1);

    private static Spool SpoolAt(string id, Station station) => new()
    {
        Id = id,
        PackageId = "PKG-1",
        SpoolNumber = id,
        DueDate = Future,
        Bom = [],
        // CurrentStation defaults to Detailing when history is empty; otherwise it is
        // the latest StatusEvent's station.
        StatusHistory = station == Station.Detailing
            ? []
            : [new StatusEvent(station, new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), "tester")]
    };

    private static async Task<DashboardDto> DashboardFor(params Spool[] spools)
    {
        var repo = new InMemorySpoolRepository();
        await repo.SeedAsync(spools);
        return await new DashboardService(repo).GetDashboardAsync();
    }

    [Fact]
    public async Task WipByStation_lists_all_six_stations_in_domain_order()
    {
        var dto = await DashboardFor(SpoolAt("s1", Station.Detailing));

        Assert.Equal(
            new[] { "Detailing", "Cut", "Weld", "QC", "Shipped", "Installed" },
            dto.WipByStation.Select(x => x.Station).ToArray());
    }

    [Fact]
    public async Task WipByStation_counts_each_spool_once_and_sums_to_total()
    {
        var spools = new[]
        {
            SpoolAt("a", Station.Detailing),
            SpoolAt("b", Station.Detailing),
            SpoolAt("c", Station.Weld),
            SpoolAt("d", Station.Installed),
            SpoolAt("e", Station.Installed),
            SpoolAt("f", Station.Installed),
        };

        var dto = await DashboardFor(spools);

        int Count(string station) => dto.WipByStation.Single(x => x.Station == station).Count;

        Assert.Equal(2, Count("Detailing"));
        Assert.Equal(0, Count("Cut"));      // zero-count station still present
        Assert.Equal(1, Count("Weld"));
        Assert.Equal(0, Count("QC"));
        Assert.Equal(0, Count("Shipped"));
        Assert.Equal(3, Count("Installed"));

        Assert.Equal(spools.Length, dto.WipByStation.Sum(x => x.Count));
    }

    [Fact]
    public async Task WipByStation_with_no_spools_reports_all_zeroes()
    {
        var dto = await DashboardFor();

        Assert.Equal(6, dto.WipByStation.Count);
        Assert.All(dto.WipByStation, s => Assert.Equal(0, s.Count));
    }
}
