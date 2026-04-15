using GridMonitor.Domain.Entities;
using GridMonitor.Domain.Shared;
using GridMonitor.Domain.ValueObjects;

namespace GridMonitor.Domain.Services;

public interface IScheduleService
{
	ValueTask<Result<int>> GetCurrentStageAsync(CancellationToken ct = default);
	ValueTask<Result<SuburbSchedule>> GetScheduleAsync(int suburbId, CancellationToken ct = default);
	ValueTask<Result<List<ScheduleSlot>>> GetUpcomingAsync(int suburbId, int currentStage, CancellationToken ct = default);
	ValueTask<Result<List<Suburb>>> SearchSuburbsAsync(string query, CancellationToken ct = default);
}
