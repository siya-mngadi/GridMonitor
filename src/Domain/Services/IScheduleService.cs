using GridMonitor.Domain.Entities;
using GridMonitor.Domain.Shared;

namespace GridMonitor.Domain.Services;

public interface IScheduleService
{
	ValueTask<Result<short>> GetCurrentStageAsync(CancellationToken ct = default);
	ValueTask<Result<SuburbSchedule>> GetScheduleAsync(int suburbId, CancellationToken ct = default);
	ValueTask<Result<IList<ScheduleSlot>>> GetUpcomingAsync(int suburbId, int currentStage, CancellationToken ct = default);
	ValueTask<Result<IList<Suburb>>> SearchSuburbsAsync(string query, CancellationToken ct = default);
}
