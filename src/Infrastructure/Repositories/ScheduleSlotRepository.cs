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

	// Returns 1 if inserted, 0 if unchanged, -1 if updated
	public async ValueTask<int> UpsertSlotAsync(ScheduleSlot slot, CancellationToken ct = default)
	{
		var existing = await GetByCompositeKeyAsync(
			slot.SuburbId, slot.Stage, slot.ScheduleDay, slot.StartTime, ct);

		if (existing is null)
		{
			await context.ScheduleSlots.AddAsync(slot, ct);
			return 1;
		}

		if (existing.DataHash == slot.DataHash) return 0;

		existing.EndTime = slot.EndTime;
		existing.DataHash = slot.DataHash;
		return -1;
	}

	public async ValueTask DeleteBySuburbAsync(int suburbId, CancellationToken ct = default)
	{
		var slots = await context.ScheduleSlots.Where(s => s.SuburbId == suburbId).ToListAsync(ct);
		context.ScheduleSlots.RemoveRange(slots);
	}
}
