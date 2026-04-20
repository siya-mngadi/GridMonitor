
using GridMonitor.Domain.Entities;

namespace GridMonitor.Domain.Repositories;

public interface IStageSnapshotRepository : IRepository
{
	ValueTask<StageSnapshot> GetLatestAsync(CancellationToken ct = default);
	ValueTask<IList<StageSnapshot>> GetAsync(CancellationToken ct = default);
	ValueTask<short> GetCurrentStageAsync(CancellationToken ct = default);
	ValueTask PurgeOlderThanAsync(TimeSpan age, CancellationToken ct = default);
	ValueTask<StageSnapshot> UpdateAsync(StageSnapshot stageSnapshot, CancellationToken ct = default);
}
