using GridMonitor.Domain.Entities;
using GridMonitor.Domain.Services;
using GridMonitor.Infrastructure.HttpClients;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GridMonitor.Infrastructure.Worker;

public class MonitorWorker : BackgroundService
{
	private readonly GridClient gridClient;
	private readonly ISuburbService gridService;
	private readonly ILogger<MonitorWorker> logger;

	public MonitorWorker(
		GridClient gridClient,
		ISuburbService gridService,
		ILogger<MonitorWorker> logger)
	{
		this.logger = logger;
		this.gridClient = gridClient;
		this.gridService = gridService;

		cron = CronExpression.Parse("0 2 * * 0"); // Every Sunday at 2 AM

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

			var syncRun = new SyncRun
			{
				Type = "EskomSync"
			};

			try
			{
				await pipeline.ExecuteAsync(async token =>
				{
					var provinces = gridService.GetProvinces();
					await foreach (var province in provinces)
					{
						var municipalites = await ProcessMunicipalities(province, stoppingToken);
						syncRun.MunicipalitiesProcessed += municipalites.Count;
						foreach (var city in municipalites)
						{
							var suburbs = await ProcessSuburbs(city, stoppingToken);
							syncRun.SuburbProcessed += suburbs.Count;

							await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
						}
						await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
					}
				}, stoppingToken);

				syncRun.Success = true;
				logger.LogInformation("Sync run completed successfully");
			}
			catch (Exception ex)
			{
				syncRun.ErrorMessage = ex.Message;
				logger.LogError(ex, "Error occurred while processing grid data.");
			}

			try
			{
				syncRun.FinishedAt = DateTime.UtcNow;
				await gridService.CreateSyncRunAsync(syncRun);
				logger.LogInformation("{syncRun}", syncRun);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error occurred completing data sync.");
			}

			await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
		}
	}

	private async ValueTask<List<Municipality>> ProcessMunicipalities(Province province, CancellationToken stoppingToken)
	{
		var municipalities = await gridClient.GetMunicipalitiesAsync(province, stoppingToken);
		var updatedCount = await gridService.UpsertMunicipalityAsync([.. municipalities]);
		logger.LogInformation("Processed {Count} municipalities for province {ProvinceName}", updatedCount, province.Name);
		return municipalities;
	}

	private async ValueTask<List<Suburb>> ProcessSuburbs(Municipality municipality, CancellationToken stoppingToken)
	{
		var suburbs = await gridClient.GetSuburbsAsync(municipality, stoppingToken);
		var updatedCount = await gridService.UpsertSuburbAsync([.. suburbs]);
		logger.LogInformation("Processed {Count} suburbs for municipality {MunicipalityName}", updatedCount, municipality.Name);
		return suburbs;
	}
}