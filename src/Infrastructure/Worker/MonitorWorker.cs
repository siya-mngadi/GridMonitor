using GridMonitor.Domain.Entities;
using GridMonitor.Domain.Services;
using GridMonitor.Infrastructure.HttpClients;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace GridMonitor.Infrastructure.Worker;

public class MonitorWorker : BackgroundService
{
	private readonly GridClient gridClient;
	private readonly ISuburbService suburbService;
	private readonly ResiliencePipeline pipeline;
	private readonly ILogger<MonitorWorker> logger;

	public MonitorWorker(
		GridClient gridClient,
		ISuburbService suburbService,
		ILogger<MonitorWorker> logger)
	{
		this.logger = logger;
		this.gridClient = gridClient;
		this.suburbService = suburbService;

		var pipeline = new ResiliencePipelineBuilder()
			.AddRetry(new RetryStrategyOptions
			{
				MaxRetryAttempts = 3,
				Delay = TimeSpan.FromMilliseconds(1000),
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
			var syncRun = new SyncRun
			{
				Type = "EskomSync"
			};

			try
			{
				await pipeline.ExecuteAsync(async token =>
				{
					// TODO: Add more granular processing and tracking of subscription slots
				}, stoppingToken);

				syncRun.Success = true;
				logger.LogInformation("Sync run completed successfully");
			}
			catch (Exception ex)
			{
				syncRun.ErrorMessage = ex.Message;
				logger.LogError(ex, "Error occurred while processing grid data.");
			}

			await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
		}
	}
}