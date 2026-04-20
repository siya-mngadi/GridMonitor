using GridMonitor.Application.Engine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GridMonitor.Application.Workers;

public class AlertCheckWorker : BackgroundService
{
	private readonly IServiceScopeFactory scopeFactory;
	private readonly ILogger<AlertCheckWorker> logger;

	private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(5);
	private static readonly TimeSpan SignalPollInterval = TimeSpan.FromSeconds(10);

	public AlertCheckWorker(IServiceScopeFactory scopeFactory, ILogger<AlertCheckWorker> logger)
	{
		this.scopeFactory = scopeFactory;
		this.logger = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		logger.LogInformation("Alert Worker started — checking every {M}m", CheckInterval.TotalMinutes);

		var nextScheduledRun = DateTime.UtcNow;

		while (!stoppingToken.IsCancellationRequested)
		{
			var now = DateTime.UtcNow;
			var signalled = StagePollWorker.StageChangedSignal;
			var scheduleDue = now >= nextScheduledRun;

			if (signalled || scheduleDue)
			{
				StagePollWorker.StageChangedSignal = false;
				if (signalled) logger.LogInformation("Stage change signal detected, running alert engine immediately");
				
				try
				{
					await RunEngineAsync(stoppingToken);
				}
				catch (Exception ex)
				{
					logger.LogError(ex, "Error running alert engine");
				}

				nextScheduledRun = DateTime.UtcNow.Add(CheckInterval);
			}

			// Poll the signal at a short interval rather than sleeping the full 5 minutes
			await Task.Delay(SignalPollInterval, stoppingToken);
		}
	}

	private async ValueTask RunEngineAsync(CancellationToken ct)
	{
		using var scope = scopeFactory.CreateScope();
		var engine = scope.ServiceProvider.GetRequiredService<AlertEngine>();
		await engine.RunAsync(ct);
	}
}
