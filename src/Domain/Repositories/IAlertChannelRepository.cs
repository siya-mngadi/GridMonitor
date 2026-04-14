using GridMonitor.Domain.Entities;

namespace GridMonitor.Domain.Repositories;

public interface IAlertChannelRepository : IRepository
{
	ValueTask<AlertChannel> GetByIdAsync(Guid id, CancellationToken ct = default);
	ValueTask<List<AlertChannel>> GetBySubscriptionAsync(Guid subscriptionId, CancellationToken ct = default);
	ValueTask AddAsync(AlertChannel channel, CancellationToken ct = default);
	ValueTask DeactivateAsync(Guid id, CancellationToken ct = default);
}