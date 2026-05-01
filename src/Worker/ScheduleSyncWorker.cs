using Cronos;
using GridMonitor.Domain.Repositories;
using GridMonitor.Infrastructure.Proxies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace GridMonitor.Worker;

public class ScheduleSyncWorker : BackgroundService
{
	private readonly IServiceScopeFactory scopeFactory;
	private readonly ILogger<ScheduleSyncWorker> logger;

	private readonly GridParser parser; 
	private readonly CronExpression cron;
	private readonly ResiliencePipeline pipeline;

	public ScheduleSyncWorker(IServiceScopeFactory scopeFactory, ILogger<ScheduleSyncWorker> logger)
	{
		this.scopeFactory = scopeFactory;
		this.logger = logger;
		parser = new GridParser(logger);

		cron = CronExpression.Parse("0 22 28 * *"); // Every 28th of every month at 22:00 UTC

		pipeline = new ResiliencePipelineBuilder()
			.AddRetry(new RetryStrategyOptions
			{
				MaxRetryAttempts = 3,
				Delay = TimeSpan.FromMinutes(5),
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
				await pipeline.ExecuteAsync(RunScheduleSyncAsync, stoppingToken);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Schedule poll cycle failed");
			}
		}
	}

	public async ValueTask RunScheduleSyncAsync(CancellationToken ct)
	{
		var scope = scopeFactory.CreateScope();
		var scraper = scope.ServiceProvider.GetRequiredService<GridClient>();
		var scheduleRepo = scope.ServiceProvider.GetRequiredService<IScheduleSlotRepository>();
		var municipalityRepo = scope.ServiceProvider.GetRequiredService<IMunicipalityRepository>();
		var suburbRepo = scope.ServiceProvider.GetRequiredService<ISuburbRepository>();	

		var municipalities = await municipalityRepo.GetListAsync(ct);

		// TODO: Sync for all stages - stage 1: [slots], stage 2: [slots],..., stage 8: [slots]
		short stage = 1; // sync for stage one for now.
		
		// TODO: structure schedule slots data -> monday - sunday, 00:00 - 24:00, stage 1/2/3 - regardless of the actual date
		foreach (var municipality in municipalities)
		{
			var suburbs = await suburbRepo.GetByMunicipalityAsync(municipality.Id, ct);
			foreach (var suburb in suburbs)
			{
				var scheduleHtml = await scraper.GetScheduleHtmlAsync(suburb.EskomId, stage, ct);
				var schedules = await parser.ParseScheduleAsync(suburb, stage, scheduleHtml, ct);

				await scheduleRepo.UpsertSlotsAsync(schedules, ct);
				await Task.Delay(TimeSpan.FromSeconds(10), ct);
			}
			await Task.Delay(TimeSpan.FromSeconds(15), ct);
		}

	}
}
