using EFCore.BulkExtensions;
using GridMonitor.Domain.Entities;
using GridMonitor.Domain.Repositories;
using GridMonitor.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;

namespace GridMonitor.Infrastructure.Repositories;

public class ScheduleSlotRepository : IScheduleSlotRepository
{
	private readonly AppDbContext context;
	public ScheduleSlotRepository(AppDbContext context)
	{
		this.context = context;
	}

	public IUnitOfWork UnitOfWork => context;

	public async ValueTask<ScheduleSlot> GetByIdAsync(int id, CancellationToken ct = default)
	{
		return await context.ScheduleSlots.FindAsync([id], ct);
	}

	public async ValueTask<ScheduleSlot> GetByCompositeKeyAsync(
	   int suburbId,
	   int stage,
	   DayOfWeek dayNumber,
	   TimeOnly startTime,
	   CancellationToken ct = default)
	{
		return await context.ScheduleSlots.FirstOrDefaultAsync(s =>
			   s.SuburbId == suburbId &&
			   s.Stage == stage &&
			   s.ScheduleDay == dayNumber &&
			   s.StartTime == startTime, ct);
	}

	public async ValueTask<List<ScheduleSlot>> GetBySuburbAndStageAsync(
		int suburbId,
		int stage,
		CancellationToken ct = default)
	{
		return await context.ScheduleSlots.Where(s => s.SuburbId == suburbId && s.Stage <= stage)
			  .OrderBy(s => s.ScheduleDay)
			  .ThenBy(s => s.StartTime)
			  .ToListAsync(ct);
	}

	public async ValueTask<List<ScheduleSlot>> GetUpcomingForSuburbAsync(
		int suburbId,
		int currentStage,
		DayOfWeek dayNumber,
		TimeOnly afterTime,
		CancellationToken ct = default)
	{
		return await context.ScheduleSlots.Where(s =>
				s.SuburbId == suburbId &&
				s.Stage <= currentStage &&
				s.ScheduleDay == dayNumber &&
				s.StartTime > afterTime)
			  .OrderBy(s => s.StartTime)
			  .ToListAsync(ct);
	}

	public async ValueTask<int> UpsertSlotsAsync(List<ScheduleSlot> slots, CancellationToken ct = default)
	{
		var config = new BulkConfig
		{
			CalculateStats = true,
			UpdateByProperties = [nameof(ScheduleSlot.SuburbId), nameof(ScheduleSlot.Stage), nameof(ScheduleSlot.ScheduleDay), nameof(ScheduleSlot.StartTime)],
			PropertiesToIncludeOnUpdate =
			[
				nameof(ScheduleSlot.EndTime),
				nameof(ScheduleSlot.DataHash)
			]
		};

		await context.BulkInsertOrUpdateAsync(slots, config, cancellationToken: ct);
		return config.StatsInfo?.StatsNumberInserted + config.StatsInfo?.StatsNumberUpdated ?? 0;
	}

	public async ValueTask DeleteBySuburbAsync(int suburbId, CancellationToken ct = default)
	{
		var slots = await context.ScheduleSlots.Where(s => s.SuburbId == suburbId).ToListAsync(ct);
		context.ScheduleSlots.RemoveRange(slots);
	}

	public async ValueTask PurgeOlderThanAsync(TimeSpan age, CancellationToken ct = default)
	{
		var cutoffDate = DateTime.UtcNow - age;
		var slots = await context.ScheduleSlots.Where(s => s.CreatedAt < cutoffDate).ToListAsync(ct);
		context.ScheduleSlots.RemoveRange(slots);
	}
}
