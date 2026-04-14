using GridMonitor.Domain.Entities;
using GridMonitor.Domain.Enums;

namespace GridMonitor.Domain.Repositories;

public interface IAlertLogRepository : IRepository
{
	ValueTask<List<AlertLog>> GetBySubscriptionAsync(Guid subscriptionId, int limit, CancellationToken ct = default);

	ValueTask<bool> WasAlertSentAsync(
		Guid subscriptionId, 
		int stage, 
		AlertEvent alertEvent,
		TimeSpan within, 
		CancellationToken ct = default);

	ValueTask AddAsync(AlertLog log, CancellationToken ct = default);
	ValueTask PurgeOlderThanAsync(TimeSpan age, CancellationToken ct = default);
}
