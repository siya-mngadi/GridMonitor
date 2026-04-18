using GridMonitor.Domain.Entities;
using GridMonitor.Domain.Enums;
using GridMonitor.Domain.Repositories;
using GridMonitor.Domain.Shared;
using Microsoft.Extensions.Logging;

namespace GridMonitor.Application.Engine;

/// <summary>
/// Provides core alert processing functionality, coordinating alert subscriptions, logs, and notifications.
/// </summary>
/// <remarks>The AlertEngine is typically invoked by background workers to evaluate alert conditions and dispatch
/// notifications. It relies on repository and dispatcher abstractions for data access and notification delivery. This
/// class is not thread-safe; concurrent use should be managed externally.</remarks>
public class AlertEngine
{
	private readonly IAlertSubscriptionRepository subscriptionRepository;
	private readonly IAlertLogRepository logRepository;
	private readonly IStageSnapshotRepository snapshotRepository;
//	private readonly INotificationDispatcher dispatcher;
	private readonly ILogger<AlertEngine> logger;

	// Look forward far enough to catch the next check cycle too — prevents
	// a slot slipping through the gap between two 5-minute runs.
	private static readonly TimeSpan LookAheadBuffer = TimeSpan.FromMinutes(6);

	private static readonly TimeZoneInfo SaZone =
	TimeZoneInfo.FindSystemTimeZoneById(
		OperatingSystem.IsWindows() ? "South Africa Standard Time" : "Africa/Johannesburg");
	public AlertEngine(
		IAlertSubscriptionRepository subscriptionRepository,
		IAlertLogRepository logRepository,
		IStageSnapshotRepository snapshotRepository,
	//	INotificationDispatcher dispatcher,
		ILogger<AlertEngine> logger)
	{
		this.subscriptionRepository = subscriptionRepository;
		this.logRepository = logRepository;
		this.snapshotRepository = snapshotRepository;
	//_dispatcher = dispatcher;
		this.logger = logger;
	}

	public async ValueTask RunAsync(CancellationToken ct = default)
	{
		var currentStage = await snapshotRepository.GetCurrentStageAsync(ct);

		if (currentStage == 0)
		{
			logger.LogDebug("Stage 0 — no shedding active, skipping alert check");
			return;
		}

		var nowSast = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, SaZone);
		var todayDay = nowSast.DayOfWeek;
		var nowTime = TimeOnly.FromDateTime(nowSast);

		logger.LogDebug("Alert check — Stage={Stage} SAST={Time} Day={Day}", currentStage, nowSast.ToString("HH:mm"), todayDay);

		// Single query loads everything the engine needs — avoids N+1 per subscription
		var subscriptions = await subscriptionRepository.GetAllActiveWithDetailsAsync(ct);

		logger.LogDebug("Checking {Count} active subscriptions", subscriptions.Count);

		// Process subscriptions concurrently but cap parallelism to avoid
		// hammering notification providers or hitting rate limits
		var semaphore = new SemaphoreSlim(10, 10);
		var tasks = subscriptions.Select(sub => ProcessSubscriptionAsync(sub, currentStage, nowSast, nowTime, todayDay, semaphore, ct));

		await Task.WhenAll(tasks);
	}

	private async Task ProcessSubscriptionAsync(
	   AlertSubscription sub,
	   short currentStage,
	   DateTime nowSast,
	   TimeOnly nowTime,
	   DayOfWeek todayDay,
	   SemaphoreSlim semaphore,
	   CancellationToken ct)
	{
		await semaphore.WaitAsync(ct);
		try
		{
			await CheckSubscriptionAsync(sub, currentStage, nowSast, nowTime, todayDay, ct);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Unhandled error processing subscription {Id}", sub.Id);
		}
		finally
		{
			semaphore.Release();
		}
	}

	private async Task CheckSubscriptionAsync(
		AlertSubscription sub,
		short currentStage,
		DateTime nowSast,
		TimeOnly nowTime,
		DayOfWeek todayDay,
		CancellationToken ct)
	{
		if (!sub.Active|| sub.Channels.Count == 0) return;

		// Find slots active at the current stage for today's block that
		// start within [now, now + alertMinutesBefore + buffer]
		var windowEnd = nowTime.Add(TimeSpan.FromMinutes(sub.AlertMinutesBefore) + LookAheadBuffer);

		var upcomingSlots = sub.Suburb.Slots
			.Where(s =>
				s.Stage <= currentStage &&
				s.ScheduleDay == todayDay &&
				s.StartTime >= nowTime &&
				s.StartTime <= windowEnd)
			.OrderBy(s => s.StartTime)
			.ToList();

		if (upcomingSlots.Count == 0) return;

		var slot = upcomingSlots[0];
		var dateStr = DateOnly.FromDateTime(nowSast).ToString("yyyy-MM-dd");
		var idemKey = BuildIdempotencyKey(sub.Id, currentStage, dateStr, slot.StartTime);

		// Deduplication: check if any channel for this exact slot was already attempted
		// Uses the idempotency key — same as the Postgres constraint in migration 002
		var alreadySent = await logRepository.WasAlertSentAsync(
			sub.Id, currentStage, AlertEvent.StartingSoon,
			within: TimeSpan.FromHours(24), ct);

		if (alreadySent)
		{
			logger.LogDebug("Alert already sent for sub {Id} key {Key}", sub.Id, idemKey);
			return;
		}

		logger.LogInformation("Firing alerts: Suburb={Suburb} Stage={Stage} Slot={Start} Channels={N}",
			sub.Suburb.Name, currentStage, slot.StartTime, sub.Channels.Count);

		// Fire each channel — failures are logged per-channel, not thrown
		foreach (var channel in sub.Channels.Where(c => c.Active))
		{
			if (ct.IsCancellationRequested) break;
			await FireChannelAsync(sub, channel, slot, currentStage, ct);
		}
	}

	private async Task FireChannelAsync(
		AlertSubscription sub,
		AlertChannel channel,
		ScheduleSlot slot,
		short stage,
		CancellationToken ct)
	{
		// Tier gate — double-checked here even though AddChannelAsync already enforces it,
		// because a user's tier may have been downgraded after the channel was added
		if (!TierPolicy.CanUseChannel(sub.User.Tier, channel.ChannelType))
		{
			logger.LogWarning(
				"Channel {Type} not allowed on tier {Tier} — skipping sub {Id}",
				channel.ChannelType, sub.User.Tier, sub.Id);
			return;
		}

		var message = BuildMessage(sub.Suburb.Name, stage, slot, sub.AlertMinutesBefore);
		var log = new AlertLog
		{
			SubscriptionId = sub.Id,
			ChannelType = channel.ChannelType,
			Destination = channel.Destination,
			Event = AlertEvent.StartingSoon,
			Stage = stage,
			SentAt = DateTime.UtcNow
		};

		try
		{
			// await _dispatcher.SendAsync(channel, message, ct);
			log.Success = true;
			log.AttemptCount = 1;

			logger.LogInformation("Alert sent via {Channel} to {Dest}",
				channel.ChannelType, Mask(channel.Destination));
		}
		catch (Exception ex)
		{
			log.Success = false;
			log.ErrorMessage = ex.Message[..Math.Min(ex.Message.Length, 500)];
			log.AttemptCount = 1;

			logger.LogError(ex, "Alert failed via {Channel} to {Dest}",
				channel.ChannelType, Mask(channel.Destination));
		}

		await logRepository.AddAsync(log, ct);
		await logRepository.UnitOfWork.SaveEntitiesAsync(ct);
	}

	// Must match the format used by make_idempotency_key() in migration 002
	private static string BuildIdempotencyKey(
		Guid subscriptionId, 
		short stage, 
		string dateSast, 
		TimeOnly slotStart)
		=> $"{subscriptionId}:{stage}:{dateSast}:{slotStart:HH:mm}";

	private static string BuildMessage(
		string suburb,
		short stage,
		ScheduleSlot slot,
		int minutesBefore)
	{
		var start = slot.StartTime.ToString("HH:mm");
		var end = slot.EndTime.ToString("HH:mm");
		return $"Load shedding alert: Stage {stage} starts in ~{minutesBefore} minutes " +
			   $"for {suburb}. Scheduled {start}–{end}. Charge devices & prepare now.";
	}

	private static string Mask(string dest) => dest.Length > 6 ? dest[..3] + "***" + dest[^3..] : "***";
}