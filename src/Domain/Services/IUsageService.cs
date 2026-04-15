using GridMonitor.Domain.Shared;
using GridMonitor.Domain.ValueObjects;

namespace GridMonitor.Domain.Services;

public interface IUsageService
{
	// Returns false if the key has hit its daily limit
	ValueTask<Result<bool>> CheckAndIncrementAsync(Guid apiKeyId, CancellationToken ct = default);
	ValueTask<Result<UsageStatsResult>> GetStatsAsync(Guid userId, CancellationToken ct = default);
}
