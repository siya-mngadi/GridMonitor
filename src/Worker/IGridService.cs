using GridMonitor.Domain.Entities;

namespace GridMonitor.Worker;

internal interface IGridService
{
	ValueTask<List<Province>> GetProvincesAsync(CancellationToken ct = default);
	ValueTask<int> UpsertMunicipalityAsync(IEnumerable<Municipality> municipalities, CancellationToken ct = default);
	ValueTask<int> UpsertSuburbAsync(IEnumerable<Suburb> suburbs, CancellationToken ct = default);
	ValueTask CreateSyncRunAsync(SyncRun syncRun, CancellationToken ct = default);
}
