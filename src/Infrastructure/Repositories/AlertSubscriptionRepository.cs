using GridMonitor.Domain.Entities;
using GridMonitor.Domain.Repositories;
using GridMonitor.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;

namespace GridMonitor.Infrastructure.Repositories;

public class AlertSubscriptionRepository : IAlertSubscriptionRepository
{
	private readonly AppDbContext context;
	public AlertSubscriptionRepository(AppDbContext context)
	{
		this.context = context;
	}

	public IUnitOfWork UnitOfWork => context;

	public async ValueTask AddAsync(AlertSubscription subscription, CancellationToken ct = default)
	{
		await context.Subscriptions.AddAsync(subscription, ct);
	}

	public async ValueTask<List<AlertSubscription>> GetAllActiveWithDetailsAsync(CancellationToken ct = default)
	{
		var subscriptions = await context.Subscriptions
			  .Where(s => s.Active)
			  .Include(s => s.User)
			  .Include(s => s.Suburb)
			  .ToListAsync(ct);

		foreach (var subscription in subscriptions)
		{
			subscription.Channels = await context.Channels.Where(c => c.SubscriptionId == subscription.Id && c.Active).ToListAsync(ct);
		}
		return subscriptions;
	}

	public ValueTask<AlertSubscription> GetByIdAsync(Guid id, CancellationToken ct = default)
	{
		return context.Subscriptions.FindAsync([id], ct);
	}

	public async ValueTask<AlertSubscription> GetByUserAndSuburbAsync(Guid userId, int suburbId, CancellationToken ct = default)
	{
		return await context.Subscriptions.FirstOrDefaultAsync(s => s.UserId == userId && s.SuburbId == suburbId, ct);
	}

	public async ValueTask<List<AlertSubscription>> GetByUserAsync(Guid userId, CancellationToken ct = default)
	{
		return await context.Subscriptions
				.AsNoTracking()
				.Where(s => s.UserId.Equals(userId))
				.Include(s => s.Channels)
				.Include(s => s.Suburb)
				.ToListAsync(ct);
	}
}
