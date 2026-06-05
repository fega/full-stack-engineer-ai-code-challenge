using StratusFabTracker.Api.Domain;

namespace StratusFabTracker.Api.Application;

public sealed class DashboardService
{
    private readonly ISpoolRepository _repository;

    public DashboardService(ISpoolRepository repository) => _repository = repository;

    public async Task<DashboardDto> GetDashboardAsync()
    {
        var spools = await _repository.GetAllAsync();
        var byStation = Enum.GetValues<Station>()
            .ToDictionary(s => s.ToString(), s => spools.Count(x => x.CurrentStation == s));

        var now = DateOnly.FromDateTime(DateTime.UtcNow);
        var pastDue = spools.Count(x => x.DueDate < now && x.CurrentStation != Station.Installed);

        return new DashboardDto(byStation, pastDue);
    }
}

public sealed record DashboardDto(Dictionary<string, int> WipByStation, int PastDueCount);
