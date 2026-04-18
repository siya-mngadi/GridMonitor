using GridMonitor.Domain.Entities;
using GridMonitor.Domain.Enums;

namespace GridMonitor.Domain.Repositories;

public interface ISyncRunRepository : IRepository
{
	ValueTask<SyncRun> GetLatestAsync(SyncEvent type, CancellationToken ct = default);
	ValueTask<List<SyncRun>> GetRecentAsync(int limit, CancellationToken ct = default);
	ValueTask AddAsync(SyncRun run, CancellationToken ct = default);
}
