using GridMonitor.Domain.Services;
using GridMonitor.Infrastructure.HttpClients;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GridMonitor.Application.Workers;

public class StagePollWorker : BackgroundService
{
	private readonly IServiceScopeFactory scopeFactory;
	private readonly ILogger<StagePollWorker> logger;

	// Shared signal: poll worker sets this when stage changes so the alert
	// worker can fire immediately rather than waiting its full 5-minute cycle
	internal static volatile bool StageChangedSignal = false;

	private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(5);
	private PeriodicTimer timer;
	public StagePollWorker(IServiceScopeFactory scopeFactory, ILogger<StagePollWorker> logger)
	{
		this.scopeFactory = scopeFactory;
		this.logger = logger;
	}
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		timer = new PeriodicTimer(PollInterval);
		while (await timer.WaitForNextTickAsync(stoppingToken)) 
		{
			try
			{
				await PollAsync(stoppingToken);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Stage poll cycle failed");
			}
		}
	}

	private async ValueTask PollAsync(CancellationToken ct)
	{
		using var scope = scopeFactory.CreateScope();
		var scraper = scope.ServiceProvider.GetRequiredService<GridClient>();
		var stageService = scope.ServiceProvider.GetRequiredService<IStageService>();

		var status = await scraper.GetStatusAsync(ct);
		if (status is null)
		{
			logger.LogWarning("Grid status fetch returned null — skipping this cycle");
			return;
		}

		var result = await stageService.RecordStageAsync(status.Stage, status.RawText, ct);

		if (result.Success && result.Value)
		{
			// Stage changed — signal the alert worker to run immediately
			StageChangedSignal = true;
			logger.LogInformation("Stage changed to {Stage} — alert check signalled", status.Stage);
		}
	}

	public override void Dispose()
	{
		GC.SuppressFinalize(this);
		timer?.Dispose();
		base.Dispose();
	}
}
