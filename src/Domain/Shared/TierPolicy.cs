using GridMonitor.Domain.Enums;

namespace GridMonitor.Domain.Shared;

public static class TierPolicy
{
	// Daily API call limits
	public static int DailyCallLimit(PricingTier tier) => tier switch
	{
		PricingTier.Free => 50,
		PricingTier.Starter => 10_000,
		PricingTier.Pro => int.MaxValue,
		_ => 50  // default to free tier limits for unknown tiers
	};

	// Max webhook endpoints per user
	public static int MaxWebhooks(PricingTier tier) => tier switch
	{
		PricingTier.Free => 0,
		PricingTier.Starter => 3,
		PricingTier.Pro => int.MaxValue,
		_ => 0
	};

	public static bool CanUseChannel(PricingTier tier, ChannelType channel) => channel switch
	{
		ChannelType.WhatsApp => true,
		ChannelType.Email => tier is PricingTier.Starter or PricingTier.Pro,
		ChannelType.Sms => tier == PricingTier.Pro,
		ChannelType.Webhook => tier is PricingTier.Starter or PricingTier.Pro,
		_ => false
	};

	// Valid lead times for alerts
	public static readonly int[] ValidAlertMinutes = [30, 60, 90];
}
