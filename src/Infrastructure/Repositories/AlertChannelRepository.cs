using GridMonitor.Domain.Entities;
using GridMonitor.Domain.Repositories;
using GridMonitor.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;

namespace GridMonitor.Infrastructure.Repositories;

public class AlertChannelRepository : IAlertChannelRepository
{
	private readonly AppDbContext context;

	public AlertChannelRepository(AppDbContext context)
	{
		this.context = context;
	}

	public IUnitOfWork UnitOfWork => context;

	public async ValueTask AddAsync(AlertChannel channel, CancellationToken ct = default)
	{
		await context.Channels.AddAsync(channel, ct);
	}

	public async ValueTask DeactivateAsync(Guid id, CancellationToken ct = default)
	{
		var channel = await GetByIdAsync(id, ct);
		channel?.Active = false;
	}

	public async ValueTask<AlertChannel> GetByIdAsync(Guid id, CancellationToken ct = default)
	{
		return await context.Channels.FindAsync([id], ct);
	}

	public async ValueTask<List<AlertChannel>> GetBySubscriptionAsync(Guid subscriptionId, CancellationToken ct = default)
	{
		return await context.Channels
			  .Where(c => c.SubscriptionId == subscriptionId && c.Active)
			  .ToListAsync(ct);
	}
}
