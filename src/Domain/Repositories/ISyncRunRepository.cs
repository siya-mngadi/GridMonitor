using GridMonitor.Domain.Entities;

namespace GridMonitor.Domain.Repositories;

public interface ISyncRunRepository : IRepository
{
	ValueTask<SyncRun> GetLatestAsync(string type, CancellationToken ct = default);
	ValueTask<List<SyncRun>> GetRecentAsync(int limit, CancellationToken ct = default);
	ValueTask AddAsync(SyncRun run, CancellationToken ct = default);
}
