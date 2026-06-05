namespace StratusFabTracker.Api.Domain;

public sealed class Spool
{
    public required string Id { get; init; }
    public required string PackageId { get; init; }
    public required string SpoolNumber { get; init; }
    public required DateOnly DueDate { get; init; }
    public required List<BomItem> Bom { get; init; } = [];
    public required List<StatusEvent> StatusHistory { get; init; } = [];

    public Station CurrentStation => StatusHistory.Count == 0
        ? Station.Detailing
        : StatusHistory.OrderByDescending(x => x.ChangedAt).First().Station;
}

public sealed record BomItem(string PartNumber, int Quantity);
public sealed record StatusEvent(Station Station, DateTimeOffset ChangedAt, string ChangedBy);
