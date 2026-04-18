using GridMonitor.Domain.Entities;

namespace GridMonitor.Domain.Repositories;

public interface IScheduleSlotRepository : IRepository
{
	ValueTask<ScheduleSlot> GetByCompositeKeyAsync(
		int suburbId,
		int stage,
		DayOfWeek dayNumber,
		TimeOnly startTime,
		CancellationToken ct = default);

	ValueTask<List<ScheduleSlot>> GetBySuburbAndStageAsync(
		int suburbId,
		int stage, 
		CancellationToken ct = default);

	ValueTask PurgeOlderThanAsync(TimeSpan age, CancellationToken ct = default);

	ValueTask<List<ScheduleSlot>> GetUpcomingForSuburbAsync(
		int suburbId,
		int currentStage,
		DayOfWeek dayNumber,
		TimeOnly afterTime,
		CancellationToken ct = default);

	ValueTask<int> UpsertSlotAsync(List<ScheduleSlot> slots, CancellationToken ct = default);
	ValueTask DeleteBySuburbAsync(int suburbId, CancellationToken ct = default);
}
