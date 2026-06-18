using StratusFabTracker.Api.Domain;

namespace StratusFabTracker.Api.Application;

/// <summary>
/// Read-side queries over spools for the dashboard UI. Kept separate from the
/// write-side <see cref="SpoolWorkflowService"/> so listing stays a pure read.
/// </summary>
public sealed class SpoolQueryService
{
    private readonly ISpoolRepository _repository;

    public SpoolQueryService(ISpoolRepository repository) => _repository = repository;

    /// <summary>
    /// All spools as flat summaries, ordered by spool number. Each summary carries
    /// its <see cref="Spool.CurrentStation"/> so the UI can group spools by station
    /// (the per-station counts on the dashboard are exactly the sizes of these groups).
    /// </summary>
    public async Task<IReadOnlyList<SpoolSummaryDto>> GetSummariesAsync()
    {
        var spools = await _repository.GetAllAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return spools
            .OrderBy(s => s.SpoolNumber, StringComparer.OrdinalIgnoreCase)
            .Select(s => new SpoolSummaryDto(
                s.Id,
                s.SpoolNumber,
                s.PackageId,
                s.DueDate,
                s.CurrentStation.ToString(),
                // Past due mirrors the dashboard: an overdue spool that has already
                // been Installed is complete, so it is not flagged.
                s.DueDate < today && s.CurrentStation != Station.Installed))
            .ToList();
    }
}

public sealed record SpoolSummaryDto(
    string Id,
    string SpoolNumber,
    string PackageId,
    DateOnly DueDate,
    string CurrentStation,
    bool IsPastDue);
