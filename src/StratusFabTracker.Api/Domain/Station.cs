namespace StratusFabTracker.Api.Domain;

public enum Station
{
    Detailing = 0,
    Cut = 1,
    Weld = 2,
    QC = 3,
    Shipped = 4,
    Installed = 5
}

public static class StationExtensions
{
    public static Station? Next(this Station station) => station switch
    {
        Station.Detailing => Station.Cut,
        Station.Cut => Station.Weld,
        Station.Weld => Station.QC,
        Station.QC => Station.Shipped,
        Station.Shipped => Station.Installed,
        Station.Installed => null,
        _ => null
    };
}
