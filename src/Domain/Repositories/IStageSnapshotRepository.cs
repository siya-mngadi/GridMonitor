
using GridMonitor.Domain.Entities;

namespace GridMonitor.Domain.Repositories;

public interface IStageSnapshotRepository : IRepository
{
	ValueTask<StageSnapshot> GetLatestAsync(CancellationToken ct = default);
	ValueTask<int> GetCurrentStageAsync(CancellationToken ct = default);
	ValueTask PurgeOlderThanAsync(TimeSpan age, CancellationToken ct = default);
	ValueTask<StageSnapshot> UpdateAsync(StageSnapshot stageSnapshot, CancellationToken ct = default);
}
