using GridMonitor.Domain.Repositories;
using GridMonitor.Domain.Services;
using GridMonitor.Infrastructure.Proxies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GridMonitor.Application.Workers;

public class SchedulePollWorker : BackgroundService
{
	private readonly IServiceScopeFactory scopeFactory;
	private readonly ILogger<SchedulePollWorker> logger;

	private readonly GridParser parser;
	private readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(5);
	private readonly TimeSpan PollInterval = TimeSpan.FromSeconds(60);

	public SchedulePollWorker(IServiceScopeFactory scopeFactory, ILogger<SchedulePollWorker> logger)
	{
		this.scopeFactory = scopeFactory;
		this.logger = logger;
		parser = new GridParser(logger);
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		logger.LogInformation("Schedule Poll Worker started — checking every {M}m", CheckInterval.TotalMinutes);

		var nextPoll = DateTime.UtcNow;
		while (!stoppingToken.IsCancellationRequested)
		{
			var signalled = StagePollWorker.StageChangedSignal;
			if (signalled) logger.LogInformation("Stage change signal detected, running schedule poll immediately");
			if (signalled || DateTime.UtcNow > nextPoll)
			{
				try
				{
					await RunSchedulePollAsync(stoppingToken);
				}
				catch (Exception ex)
				{
					logger.LogError(ex, "Schedule poll cycle failed");
				}

				nextPoll = DateTime.UtcNow + CheckInterval;
			}

			await Task.Delay(PollInterval, stoppingToken);
		}
	}

	public async ValueTask RunSchedulePollAsync(CancellationToken ct)
	{
		var scope = scopeFactory.CreateScope();
		var scraper = scope.ServiceProvider.GetRequiredService<GridClient>();
		var scheduleRepo= scope.ServiceProvider.GetRequiredService<IScheduleSlotRepository>();
		var subscriptionRepo = scope.ServiceProvider.GetRequiredService<IAlertSubscriptionRepository>();


		var subscriptions = await subscriptionRepo.GetAllActiveWithDetailsAsync(ct);

		var stage = (await scraper.GetStatusAsync(ct)).Stage;

		foreach (var subscription in subscriptions)
		{
			var suburb = subscription.Suburb;
			var scheduleHtml = await scraper.GetScheduleHtmlAsync(suburb.EskomId, stage, ct);
			var schedules = await parser.ParseScheduleAsync(suburb, stage, scheduleHtml, ct);

			await scheduleRepo.UpsertSlotsAsync(schedules, ct);
			
			await Task.Delay(TimeSpan.FromSeconds(10), ct);
		}

	}
}
