using GridMonitor.Domain.Shared;

namespace GridMonitor.Domain.Services;

public interface IStageService
{
	ValueTask<Result<bool>> RecordStageAsync(short stage, string rawValue, CancellationToken ct = default);

	ValueTask PurgeOldSnapshotsAsync(CancellationToken ct = default);
}
