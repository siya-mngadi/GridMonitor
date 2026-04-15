using GridMonitor.Domain.Entities;
using GridMonitor.Domain.Repositories;
using GridMonitor.Domain.Services;
using GridMonitor.Domain.Shared;
using Microsoft.Extensions.Logging;

namespace GridMonitor.Application.Services;

public class StageService : IStageService
{
	private readonly IStageSnapshotRepository snapshotRepository;
	private readonly ILogger<StageService> logger;
	public StageService(IStageSnapshotRepository snapshotRepository, ILogger<StageService> logger)
	{
		this.snapshotRepository = snapshotRepository;
		this.logger = logger;
	}

	public async ValueTask<Result<bool>> RecordStageAsync(short stage, string rawValue, CancellationToken ct = default)
	{
		if (stage < 0 || stage > 8)
			return Result<bool>.Fail($"Stage must be 0–8. Got {stage}.");

		var current = await snapshotRepository.GetCurrentStageAsync(ct);
		var changed = current != stage;

		var snapshot = new StageSnapshot
		{
			Stage = stage,
			RawText = rawValue,
			CreatedAt = DateTime.UtcNow
		};

		await snapshotRepository.UpdateAsync(snapshot, ct);
		await snapshotRepository.UnitOfWork.SaveEntitiesAsync(ct);

		if (changed)
			logger.LogInformation("Stage changed: {Old} → {New}", current, stage);

		// We return whether the stage changed, so that callers can decide if they need to trigger any updates.
		return Result<bool>.Ok(changed);
	}

	public ValueTask PurgeOldSnapshotsAsync(CancellationToken ct = default)
	{
		return snapshotRepository.PurgeOlderThanAsync(TimeSpan.FromDays(7), ct);
	}
}
