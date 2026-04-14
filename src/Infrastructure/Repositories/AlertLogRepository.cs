using GridMonitor.Domain.Entities;
using GridMonitor.Domain.Enums;
using GridMonitor.Domain.Repositories;
using GridMonitor.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;

namespace GridMonitor.Infrastructure.Repositories;

public class AlertLogRepository : IAlertLogRepository
{
	private readonly AppDbContext context;
	public AlertLogRepository(AppDbContext context)
	{
		this.context = context;
	}
	public IUnitOfWork UnitOfWork => context;

	public async ValueTask AddAsync(AlertLog log, CancellationToken ct = default)
	{
		await context.AlertLogs.AddAsync(log, ct);
	}

	public async ValueTask<List<AlertLog>> GetBySubscriptionAsync(Guid subscriptionId, int limit, CancellationToken ct = default)
	{
		return await context.AlertLogs
			  .Where(l => l.SubscriptionId == subscriptionId)
			  .OrderByDescending(l => l.SentAt)
			  .Take(limit)
			  .ToListAsync(ct);
	}

	public async ValueTask PurgeOlderThanAsync(TimeSpan age, CancellationToken ct = default)
	{
		var cutoff = DateTime.UtcNow - age;
		var old = await context.AlertLogs.Where(l => l.SentAt < cutoff).ToListAsync(ct);
		context.AlertLogs.RemoveRange(old);
	}

	public async ValueTask<bool> WasAlertSentAsync(
		Guid subscriptionId,
		int stage,
		AlertEvent alertEvent,
		TimeSpan within,
		CancellationToken ct = default)
	{
		return await context.AlertLogs.AnyAsync(l =>
				l.SubscriptionId == subscriptionId &&
				l.Stage == stage &&
				l.Event == alertEvent &&
				l.Success &&
				l.SentAt > DateTime.UtcNow - within, ct);
	}
}
