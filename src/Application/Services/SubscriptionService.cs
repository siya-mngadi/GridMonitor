using GridMonitor.Domain.Entities;
using GridMonitor.Domain.Enums;
using GridMonitor.Domain.Repositories;
using GridMonitor.Domain.Services;
using GridMonitor.Domain.Shared;
using Microsoft.Extensions.Logging;

namespace GridMonitor.Application.Services;

public class SubscriptionService : ISubscriptionService
{
	private readonly IAlertSubscriptionRepository subscriptionRepository;
	private readonly IAlertChannelRepository channelRepository;
	private readonly ISuburbRepository suburbRepository;
	private readonly IUserRepository userRepository;
	private readonly ILogger<SubscriptionService> logger;

	public SubscriptionService(
		IAlertSubscriptionRepository subscriptionRepository,
		IAlertChannelRepository channelRepository,
		ISuburbRepository suburbRepository,
		IUserRepository userRepository,
		ILogger<SubscriptionService> logger)
	{
		this.subscriptionRepository = subscriptionRepository;
		this.channelRepository = channelRepository;
		this.suburbRepository = suburbRepository;
		this.userRepository = userRepository;
		this.logger = logger;
	}

	public async ValueTask<Result<AlertChannel>> AddChannelAsync(Guid userId, Guid subscriptionId, ChannelType type, string destination, CancellationToken ct = default)
	{
		// Validate user + tier
		var user = await userRepository.GetByIdAsync(userId, ct);
		if (user is null) return Result<AlertChannel>.Fail("User not found.");
		if (!user.Active) return Result<AlertChannel>.Fail("Account is deactivated.");

		// Enforce tier channel permissions
		if (!TierPolicy.CanUseChannel(user.Tier, type))
			return Result<AlertChannel>.Fail($"{type} alerts require a higher plan. Current plan: {user.Tier}.");

		// Validate subscription ownership
		var sub = await subscriptionRepository.GetByIdAsync(subscriptionId, ct);
		if (sub is null) return Result<AlertChannel>.Fail("Subscription not found.");
		if (sub.UserId != userId) return Result<AlertChannel>.Fail("Access denied.");
		if (!sub.Active) return Result<AlertChannel>.Fail("Subscription is not active.");

		// Validate destination
		var validationError = ValidateDestination(type, destination);
		if (validationError is not null)
			return Result<AlertChannel>.Fail(validationError);

		// Prevent duplicate channels of the same type+destination on one subscription
		var existing = await channelRepository.GetBySubscriptionAsync(subscriptionId, ct);
		if (existing.Any(c => c.ChannelType == type && c.Destination == destination))
			return Result<AlertChannel>.Fail("A channel with that type and destination already exists on this subscription.");

		// Webhooks get an HMAC secret generated automatically
		string webhookSecret = null;
		if (type == ChannelType.Webhook)
			webhookSecret = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));

		var channel = new AlertChannel
		{
			SubscriptionId = subscriptionId,
			ChannelType = type,
			Destination = destination.Trim(),
			WebhookSecret = webhookSecret,
			Active = true
		};

		await channelRepository.AddAsync(channel, ct);
		await channelRepository.UnitOfWork.SaveEntitiesAsync(ct);
		logger.LogInformation("Channel {Type} added to subscription {SubId} for user {UserId}", type, subscriptionId, userId);

		return Result<AlertChannel>.Ok(channel);
	}

	public async ValueTask<Result<List<AlertSubscription>>> GetUserSubscriptionsAsync(Guid userId, CancellationToken ct = default)
	{
		var result = await subscriptionRepository.GetByUserAsync(userId, ct);
		return Result<List<AlertSubscription>>.Ok(result);
	}

	public async ValueTask<Result> RemoveChannelAsync(Guid userId, Guid channelId, CancellationToken ct = default)
	{
		var channel = await channelRepository.GetByIdAsync(channelId, ct);
		if (channel is null) return Result.Fail("Channel not found.");

		// Verify ownership via the subscription
		var sub = await subscriptionRepository.GetByIdAsync(channel.SubscriptionId, ct);
		if (sub is null || sub.UserId != userId)
			return Result.Fail("Access denied.");

		await channelRepository.DeactivateAsync(channelId, ct);
		await channelRepository.UnitOfWork.SaveEntitiesAsync(ct);

		return Result.Ok();
	}

	public async ValueTask<Result<AlertSubscription>> SubscribeAsync(Guid userId, int suburbId, short alertMinutesBefore, CancellationToken ct = default)
	{
		// Validate lead time
		if (!TierPolicy.ValidAlertMinutes.Contains(alertMinutesBefore))
			return Result<AlertSubscription>.Fail(
				$"Alert window must be 30, 60, or 90 minutes. Got {alertMinutesBefore}.");

		// Validate suburb exists
		var suburb = await suburbRepository.GetByIdAsync(suburbId, ct);
		if (suburb is null)
			return Result<AlertSubscription>.Fail("Suburb not found.");

		// Validate user
		var user = await userRepository.GetByIdAsync(userId, ct);
		if (user is null) return Result<AlertSubscription>.Fail("User not found.");
		if (!user.Active) return Result<AlertSubscription>.Fail("Account is deactivated.");

		// Enforce one subscription per user+suburb
		var existing = await subscriptionRepository.GetByUserAndSuburbAsync(userId, suburbId, ct);
		if (existing is not null)
		{
			// If it was soft-deleted, reactivate rather than reject
			if (!existing.Active)
			{
				existing.Active = true;
				existing.AlertMinutesBefore = alertMinutesBefore;
				await subscriptionRepository.UnitOfWork.SaveEntitiesAsync(ct);
				return Result<AlertSubscription>.Ok(existing);
			}
			return Result<AlertSubscription>.Fail(
				"You already have an active subscription for this suburb.");
		}

		var subscription = new AlertSubscription
		{
			UserId = userId,
			SuburbId = suburbId,
			AlertMinutesBefore = alertMinutesBefore,
			Active = true,
			CreatedAt = DateTime.UtcNow
		};

		await subscriptionRepository.AddAsync(subscription, ct);
		await subscriptionRepository.UnitOfWork.SaveEntitiesAsync(ct);

		logger.LogInformation("User {UserId} subscribed to suburb {SuburbId}", userId, suburbId);
		return Result<AlertSubscription>.Ok(subscription);
	}

	public async ValueTask<Result> UnsubscribeAsync(Guid userId, Guid subscriptionId, CancellationToken ct = default)
	{
		var sub = await subscriptionRepository.GetByIdAsync(subscriptionId, ct);
		if (sub is null) return Result.Fail("Subscription not found.");
		if (sub.UserId != userId) return Result.Fail("Access denied.");
		if (!sub.Active) return Result.Fail("Subscription is already inactive.");

		// Soft delete — preserve alert history
		sub.Active = false;
		await subscriptionRepository.UnitOfWork.SaveEntitiesAsync(ct);

		logger.LogInformation("User {UserId} unsubscribed from {SubId}", userId, subscriptionId);
		return Result.Ok();
	}

	public async ValueTask<Result> UpdateAlertWindowAsync(Guid userId, Guid subscriptionId, short alertMinutesBefore, CancellationToken ct = default)
	{
		if (!TierPolicy.ValidAlertMinutes.Contains(alertMinutesBefore))
			return Result.Fail($"Alert window must be 15, 30, or 60 minutes. Got {alertMinutesBefore}.");

		var sub = await subscriptionRepository.GetByIdAsync(subscriptionId, ct);
		if (sub is null) return Result.Fail("Subscription not found.");
		if (sub.UserId != userId) return Result.Fail("Access denied.");
		if (!sub.Active) return Result.Fail("Subscription is not active.");

		sub.AlertMinutesBefore = alertMinutesBefore;
		await subscriptionRepository.UnitOfWork.SaveEntitiesAsync(ct);
		return Result.Ok();
	}

	private static string ValidateDestination(ChannelType type, string destination)
	{
		if (string.IsNullOrWhiteSpace(destination))
			return "Destination cannot be empty.";

		return type switch
		{
			ChannelType.WhatsApp or ChannelType.Sms =>
				IsValidSaPhone(destination) ? null
					: "Phone number must be a valid SA number (e.g. +27821234567 or 0821234567).",

			ChannelType.Email =>
				destination.Contains('@') && destination.Contains('.') ? null
					: "Invalid email address.",

			ChannelType.Webhook =>
				Uri.TryCreate(destination, UriKind.Absolute, out var uri) &&
				(uri.Scheme == "https" || uri.Scheme == "http") ? null
					: "Webhook URL must be a valid HTTP or HTTPS URL.",

			_ => "Unknown channel type."
		};
	}

	private static bool IsValidSaPhone(string phone)
	{
		var digits = new string([.. phone.Where(char.IsDigit)]);
		// SA mobile: 10 digits starting with 0, or 11 digits starting with 27
		return (digits.Length == 10 && digits.StartsWith('0')) ||
			   (digits.Length == 11 && digits.StartsWith("27")) ||
			   (phone.StartsWith('+') && digits.Length == 11 && digits.StartsWith("27"));
	}
}
