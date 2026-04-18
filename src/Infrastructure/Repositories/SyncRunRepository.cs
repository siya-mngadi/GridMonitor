using GridMonitor.Domain.Entities;
using GridMonitor.Domain.Enums;
using GridMonitor.Domain.Repositories;
using GridMonitor.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;

namespace GridMonitor.Infrastructure.Repositories;

public class SyncRunRepository : ISyncRunRepository
{
	private readonly AppDbContext context;

	public SyncRunRepository(AppDbContext context)
	{
		this.context = context;
	}

	public IUnitOfWork UnitOfWork => context;

	public async ValueTask AddAsync(SyncRun syncRun, CancellationToken ct)
	{
		await context.SyncRuns.AddAsync(syncRun, ct);
	}

	public async ValueTask<SyncRun> GetLatestAsync(SyncEvent type, CancellationToken ct = default)
	{
		return await context.SyncRuns
			  .Where(r => r.Type == type)
			  .OrderByDescending(r => r.StartedAt)
			  .FirstOrDefaultAsync(ct);
	}

	public async ValueTask<List<SyncRun>> GetRecentAsync(int limit, CancellationToken ct = default)
	{
		return await context.SyncRuns
			  .OrderByDescending(r => r.StartedAt)
			  .Take(limit)
			  .ToListAsync(ct);
	}
}
