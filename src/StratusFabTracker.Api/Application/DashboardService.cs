using StratusFabTracker.Api.Domain;

namespace StratusFabTracker.Api.Application;

public sealed class DashboardService
{
    private readonly ISpoolRepository _repository;

    public DashboardService(ISpoolRepository repository) => _repository = repository;

    public async Task<DashboardDto> GetDashboardAsync()
    {
        var spools = await _repository.GetAllAsync();

        // Emit every station in domain order (Detailing -> ... -> Installed),
        // including zero-count stations. Each spool has exactly one CurrentStation,
        // so these counts partition the spool set and sum to the total.
        var wipByStation = Enum.GetValues<Station>()
            .OrderBy(s => (int)s)
            .Select(s => new WipStationCount(s.ToString(), spools.Count(x => x.CurrentStation == s)))
            .ToList();

        var now = DateOnly.FromDateTime(DateTime.UtcNow);
        var pastDue = spools.Count(x => x.DueDate < now && x.CurrentStation != Station.Installed);

        return new DashboardDto(wipByStation, pastDue);
    }
}

public sealed record WipStationCount(string Station, int Count);

public sealed record DashboardDto(IReadOnlyList<WipStationCount> WipByStation, int PastDueCount);
