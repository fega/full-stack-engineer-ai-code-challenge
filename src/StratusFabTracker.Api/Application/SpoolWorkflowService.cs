using StratusFabTracker.Api.Domain;

namespace StratusFabTracker.Api.Application;

public enum TransitionResult
{
    Success,
    NotFound,
    InvalidTransition
}

/// <summary>
/// Outcome of an advance attempt. <see cref="NewStation"/> is the station the spool
/// moved to on success, and null otherwise.
/// </summary>
public sealed record AdvanceOutcome(TransitionResult Result, Station? NewStation = null);

public sealed class SpoolWorkflowService
{
    private readonly ISpoolRepository _repository;
    private readonly IClock _clock;

    public SpoolWorkflowService(ISpoolRepository repository, IClock clock)
    {
        _repository = repository;
        _clock = clock;
    }

    public async Task<AdvanceOutcome> AdvanceAsync(string spoolId)
    {
        var spool = await _repository.GetByIdAsync(spoolId);
        if (spool is null) return new AdvanceOutcome(TransitionResult.NotFound);

        var next = spool.CurrentStation.Next();
        if (next is null) return new AdvanceOutcome(TransitionResult.InvalidTransition);

        spool.StatusHistory.Add(new StatusEvent(next.Value, _clock.UtcNow, "system"));
        await _repository.UpdateAsync(spool);
        return new AdvanceOutcome(TransitionResult.Success, next.Value);
    }
}
