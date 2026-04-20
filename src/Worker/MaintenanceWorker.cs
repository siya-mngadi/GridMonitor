using Cronos;
using GridMonitor.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace GridMonitor.Worker;

public class MaintenanceWorker : BackgroundService
{
	private readonly IServiceScopeFactory scopeFactory;
	private readonly ILogger<MaintenanceWorker> logger;

	private readonly ResiliencePipeline pipeline;
	private readonly CronExpression cron;
	public MaintenanceWorker(IServiceScopeFactory scopeFactory, ILogger<MaintenanceWorker> logger)
	{
		this.scopeFactory = scopeFactory;
		this.logger = logger;

		cron = CronExpression.Parse("0 0 * * *"); // Every day at 00:00 UTC = 02:00 SAST

		pipeline = new ResiliencePipelineBuilder()
			.AddRetry(new RetryStrategyOptions
			{
				MaxRetryAttempts = 3,
				Delay = TimeSpan.FromMinutes(1),
				BackoffType = DelayBackoffType.Linear,
				OnRetry = args =>
				{
					logger.LogWarning(
						"Retry {Attempt} due to {Error}",
						args.AttemptNumber,
						args.Outcome.Exception?.Message);
					return ValueTask.CompletedTask;
				}
			})
			.Build();
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			var next = cron.GetNextOccurrence(DateTimeOffset.UtcNow, TimeZoneInfo.Utc);
			var delay = (next ?? DateTimeOffset.UtcNow) - DateTimeOffset.UtcNow;

			if (delay > TimeSpan.Zero)
			{
				logger.LogInformation("Next maintenance run at: {Next}", next);
				await Task.Delay(delay, stoppingToken);
			}

			try
			{
				await pipeline.ExecuteAsync(RunMaintenanceAsync, stoppingToken);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Maintenance cycle failed");
			}
		}
	}

	private async ValueTask RunMaintenanceAsync(CancellationToken ct)
	{
		using var scope = scopeFactory.CreateScope();
		var snapshotRepo = scope.ServiceProvider.GetRequiredService<IStageSnapshotRepository>();
		var alertLogRepo = scope.ServiceProvider.GetRequiredService<IAlertLogRepository>();
		var scheduleRepo = scope.ServiceProvider.GetRequiredService<IScheduleSlotRepository>();

		logger.LogInformation("Running maintenance");

		await scheduleRepo.PurgeOlderThanAsync(TimeSpan.FromDays(1), ct);
		await scheduleRepo.UnitOfWork.SaveEntitiesAsync(ct);

		await snapshotRepo.PurgeOlderThanAsync(TimeSpan.FromDays(3), ct);
		await snapshotRepo.UnitOfWork.SaveEntitiesAsync(ct);

		await alertLogRepo.PurgeOlderThanAsync(TimeSpan.FromDays(7), ct);
		await alertLogRepo.UnitOfWork.SaveEntitiesAsync(ct);

		logger.LogInformation("Maintenance complete");
	}
}
