using GridMonitor.Domain.Entities;
using GridMonitor.Domain.Repositories;
using GridMonitor.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;

namespace GridMonitor.Infrastructure.Repositories;

public class StageSnapshotRepository : IStageSnapshotRepository
{
	private readonly AppDbContext context;
	public StageSnapshotRepository(AppDbContext context)
	{
		this.context = context;
	}

	public IUnitOfWork UnitOfWork => context;

	public async ValueTask<short> GetCurrentStageAsync(CancellationToken ct = default)
	{
		return (await GetLatestAsync(ct))?.Stage ?? 0;
	}

	public async ValueTask<IList<StageSnapshot>> GetAsync(CancellationToken ct = default)
	{
		return await context.StageSnapshots
			.OrderByDescending(s => s.CreatedAt)
			.ToListAsync(ct);
	}

	public async ValueTask<StageSnapshot> GetLatestAsync(CancellationToken ct = default)
	{
		return await context.StageSnapshots
			.OrderByDescending(s => s.CreatedAt)
			.FirstOrDefaultAsync(ct);
	}

	public async ValueTask PurgeOlderThanAsync(TimeSpan age, CancellationToken ct = default)
	{
		var cutoff = DateTime.UtcNow - age;
		var old = await context.StageSnapshots.Where(s => s.CreatedAt < cutoff).ToListAsync(ct);
		context.StageSnapshots.RemoveRange(old);
	}

	public async ValueTask<StageSnapshot> UpdateAsync(StageSnapshot stageSnapshot, CancellationToken ct = default)
	{
		// Take advantage of the created date index
		var start = new DateTime(
			stageSnapshot.CreatedAt.Year,
			stageSnapshot.CreatedAt.Month,
			stageSnapshot.CreatedAt.Day,
			0, 0, 0, DateTimeKind.Utc);

		var end = start.AddDays(1);

		var existing = await context.StageSnapshots.FirstOrDefaultAsync(s => s.Stage == stageSnapshot.Stage && s.CreatedAt >= start && s.CreatedAt < end, ct);

		if (existing == null)
		{
			await context.StageSnapshots.AddAsync(stageSnapshot, ct);
		}
		else
		{
			existing.Stage = stageSnapshot.Stage;
			existing.CreatedAt = stageSnapshot.CreatedAt;
		}

		return stageSnapshot;
	}
}
