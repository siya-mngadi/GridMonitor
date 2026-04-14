using GridMonitor.Domain.Entities;

namespace GridMonitor.Domain.Repositories;

public interface IAlertSubscriptionRepository : IRepository
{
	ValueTask<AlertSubscription> GetByIdAsync(Guid id, CancellationToken ct = default);

	ValueTask<AlertSubscription> GetByUserAndSuburbAsync(Guid userId, int suburbId, CancellationToken ct = default);

	ValueTask<List<AlertSubscription>> GetByUserAsync(Guid userId, CancellationToken ct = default);

	// Used by the alert engine every 5 minutes
	ValueTask<List<AlertSubscription>> GetAllActiveWithDetailsAsync(CancellationToken ct = default);

	ValueTask AddAsync(AlertSubscription subscription, CancellationToken ct = default);
}
