using Cronos;
using GridMonitor.Domain.Entities;
using GridMonitor.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace GridMonitor.Worker;

internal class GridWorker : BackgroundService
{
	private readonly IServiceScopeFactory scopeFactory;
	private readonly ILogger<GridWorker> logger;

	private readonly ResiliencePipeline pipeline;
	private readonly CronExpression cron;
	public GridWorker(IServiceScopeFactory scopeFactory, ILogger<GridWorker> logger)
	{
		this.scopeFactory = scopeFactory;
		this.logger = logger;

		cron = CronExpression.Parse("0 22 * * 0"); // Every Sunday 22:00 UTC = Monday 00:00 SAST

		pipeline = new ResiliencePipelineBuilder()
			.AddRetry(new RetryStrategyOptions
			{
				MaxRetryAttempts = 3,
				Delay = TimeSpan.FromSeconds(10),
				BackoffType = DelayBackoffType.Exponential,
				OnRetry = args =>
				{
					logger.LogWarning(
						"Retry {Attempt} due to {Error}",
						args.AttemptNumber,
						args.Outcome.Exception?.Message);
					return ValueTask.CompletedTask;
				}
			}).AddCircuitBreaker(new CircuitBreakerStrategyOptions
			{
				FailureRatio = 0.5,
				BreakDuration = TimeSpan.FromMinutes(20),
				MinimumThroughput = 5
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
				logger.LogInformation("Next run at: {Next}", next);
				await Task.Delay(delay, stoppingToken);
			}

			try
			{
				await pipeline.ExecuteAsync(RunFullAsync, stoppingToken);
				logger.LogInformation("Sync run completed successfully");
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error occurred completing data sync.");
			}
			await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
		}
	}

	private async ValueTask RunFullAsync(CancellationToken stoppingToken)
	{
		using var scope = scopeFactory.CreateScope();
		var client = scope.ServiceProvider.GetRequiredService<GridClient>();
		var gridService = scope.ServiceProvider.GetRequiredService<IGridService>();

		var syncRun = new SyncRun
		{
			Type = SyncEvent.FullSync,
			StartedAt = DateTime.UtcNow
		};

		try
		{
			var provinces = await gridService.GetProvincesAsync(stoppingToken);
			foreach (var province in provinces)
			{
				var municipalities = await client.GetMunicipalitiesAsync(province, stoppingToken);
				var updatedMunicipalities = await gridService.UpsertMunicipalityAsync(municipalities, stoppingToken);
				logger.LogInformation("Processed {Count} municipalities for province {ProvinceName}", updatedMunicipalities, province.Name);
				syncRun.MunicipalitiesProcessed += municipalities.Count;
				
				foreach (var municipality in municipalities)
				{
					var suburbs = await client.GetSuburbsAsync(municipality, stoppingToken);
					var updatedSuburbs = await gridService.UpsertSuburbAsync([.. suburbs]);
					logger.LogInformation("Processed {Count} suburbs for municipality {MunicipalityName}", updatedSuburbs, municipality.Name);
					syncRun.SuburbProcessed += suburbs.Count;
					await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
				}
				await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
			}

			logger.LogInformation("Municipalities and Suburbs synced successfully");

			syncRun.Success = true;
			syncRun.FinishedAt = DateTime.UtcNow;
		}
		catch (Exception ex)
		{
			syncRun.ErrorMessage = ex.Message;
			syncRun.FinishedAt = DateTime.UtcNow;
			logger.LogError(ex, "Error occurred while processing grid data.");
		}
		finally
		{
			await gridService.CreateSyncRunAsync(syncRun, stoppingToken);
			logger.LogInformation("{syncRun}", syncRun.ToString());
		}
	}
}
