using StratusFabTracker.Api.Domain;

namespace StratusFabTracker.Api.Application;

public enum TransitionResult
{
    Success,
    NotFound,
    InvalidTransition
}

public sealed class SpoolWorkflowService
{
    private readonly ISpoolRepository _repository;
    private readonly IClock _clock;

    public SpoolWorkflowService(ISpoolRepository repository, IClock clock)
    {
        _repository = repository;
        _clock = clock;
    }

    public async Task<TransitionResult> AdvanceAsync(string spoolId)
    {
        var spool = await _repository.GetByIdAsync(spoolId);
        if (spool is null) return TransitionResult.NotFound;

        var next = spool.CurrentStation.Next();
        if (next is null) return TransitionResult.InvalidTransition;

        spool.StatusHistory.Add(new StatusEvent(next.Value, _clock.UtcNow, "system"));
        await _repository.UpdateAsync(spool);
        return TransitionResult.Success;
    }
}
