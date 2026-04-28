using GridMonitor.Domain.Entities;
using GridMonitor.Domain.Repositories;
using GridMonitor.Domain.Services;
using GridMonitor.Domain.Shared;
using Microsoft.Extensions.Logging;

namespace GridMonitor.Application.Services;

public class ScheduleService : IScheduleService
{
	private readonly IStageSnapshotRepository snapshotRepository;
	private readonly IScheduleSlotRepository slotRepository;
	private readonly ISuburbRepository suburbRepository;
	private readonly ILogger<ScheduleService> logger;

	private static readonly TimeZoneInfo SaZone =
		TimeZoneInfo.FindSystemTimeZoneById(OperatingSystem.IsWindows() ? "South Africa Standard Time" : "Africa/Johannesburg");
	public ScheduleService(
		IStageSnapshotRepository snapshotRepository,
		IScheduleSlotRepository slotRepository,
		ISuburbRepository suburbRepository,
		ILogger<ScheduleService> logger)
	{
		this.snapshotRepository = snapshotRepository;
		this.slotRepository = slotRepository;
		this.suburbRepository = suburbRepository;
		this.logger = logger;
	}

	public async ValueTask<Result<short>> GetCurrentStageAsync(CancellationToken ct = default)
	{
		var stage = await snapshotRepository.GetCurrentStageAsync(ct);
		return Result<short>.Ok(stage);
	}

	public async ValueTask<Result<SuburbSchedule>> GetScheduleAsync(int suburbId, CancellationToken ct = default)
	{
		var suburb = await suburbRepository.GetByIdAsync(suburbId, ct);
		if (suburb is null)
			return Result<SuburbSchedule>.Fail("Suburb not found.");

		var currentStage = await snapshotRepository.GetCurrentStageAsync(ct);
		var allSlots = await slotRepository.GetBySuburbAndStageAsync(suburbId, currentStage, ct);

		var response = new SuburbSchedule(
			suburb.Name,
			currentStage,
			allSlots
		);

		return Result<SuburbSchedule>.Ok(response);
	}

	public async ValueTask<Result<IList<ScheduleSlot>>> GetUpcomingAsync(int suburbId, int currentStage, CancellationToken ct = default)
	{
		var suburb = await suburbRepository.GetByIdAsync(suburbId, ct);
		if (suburb is null)
			return Result<IList<ScheduleSlot>>.Fail("Suburb not found.");
		var nowSast = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, SaZone);
		var nowTime = TimeOnly.FromDateTime(nowSast);

		var slots = await slotRepository.GetUpcomingForSuburbAsync(
			suburbId, currentStage, nowSast.DayOfWeek, nowTime, ct);

		return Result<IList<ScheduleSlot>>.Ok(slots);
	}

	public async ValueTask<Result<IList<Suburb>>> SearchSuburbsAsync(string query, CancellationToken ct = default)
	{
		if(string.IsNullOrWhiteSpace(query) || query.Length < 2)
			return Result<IList<Suburb>>.Fail("Query must be at least 2 characters long.");

		var results = await suburbRepository.SearchAsync(query.Trim(), limit: 20, ct);
		return Result<IList<Suburb>>.Ok(results);
	}
}
