using StratusFabTracker.Api.Domain;
using Xunit;

namespace StratusFabTracker.Tests;

public class StationTests
{
    [Fact]
    public void Detailing_advances_to_Cut()
    {
        Assert.Equal(Station.Cut, Station.Detailing.Next());
    }

    [Fact]
    public void Installed_has_no_next_station()
    {
        Assert.Null(Station.Installed.Next());
    }
}
