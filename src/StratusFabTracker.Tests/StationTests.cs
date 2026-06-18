using StratusFabTracker.Api.Domain;
using Xunit;

namespace StratusFabTracker.Tests;

/// <summary>
/// Pins <see cref="StationExtensions.Next"/> — the single source of truth for the
/// legal forward edge <c>Detailing → Cut → Weld → QC → Shipped → Installed</c>.
///
/// The advance API only ever moves a spool via <c>Next()</c>, so "no backward move"
/// and "no skipping a station" are not expressible at the HTTP/service layer. They
/// are instead guaranteed here: every non-terminal station advances by exactly one
/// ordinal step (never backward, never skipping), and the terminal station has no
/// successor (so a spool cannot move beyond Installed).
/// </summary>
public class StationTests
{
    [Theory]
    [InlineData(Station.Detailing, Station.Cut)]
    [InlineData(Station.Cut, Station.Weld)]
    [InlineData(Station.Weld, Station.QC)]
    [InlineData(Station.QC, Station.Shipped)]
    [InlineData(Station.Shipped, Station.Installed)]
    public void Next_returns_the_immediate_successor_for_every_non_terminal_station(Station from, Station expected)
    {
        Assert.Equal(expected, from.Next());
    }

    [Fact]
    public void Installed_is_terminal_and_has_no_next_station()
    {
        // No successor => the service rejects advancing a spool already at Installed,
        // i.e. nothing can move beyond the end of the chain.
        Assert.Null(Station.Installed.Next());
    }

    [Theory]
    [InlineData(Station.Detailing)]
    [InlineData(Station.Cut)]
    [InlineData(Station.Weld)]
    [InlineData(Station.QC)]
    [InlineData(Station.Shipped)]
    public void Next_advances_by_exactly_one_ordinal_step_so_it_never_goes_backward_or_skips(Station from)
    {
        var next = from.Next();

        Assert.NotNull(next);
        // Exactly +1 in enum order rules out both backward moves (<= from) and
        // skip-ahead moves (>= from + 2) in one assertion.
        Assert.Equal((int)from + 1, (int)next!.Value);
    }
}
