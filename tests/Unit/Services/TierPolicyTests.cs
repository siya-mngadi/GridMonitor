using FluentAssertions;
using GridMonitor.Domain.Enums;
using GridMonitor.Domain.Shared;

namespace GridMonitor.Tests.Unit.Services;

public class TierPolicyTests
{
	[Theory]
	[InlineData(PricingTier.Free, 50)]
	[InlineData(PricingTier.Starter, 10_000)]
	[InlineData(PricingTier.Pro, int.MaxValue)]
	public void DailyCallLimit_CorrectPerTier(PricingTier tier, int expected)
	{
		TierPolicy.DailyCallLimit(tier).Should().Be(expected);
	}

	[Theory]
	[InlineData(PricingTier.Free, 0)]
	[InlineData(PricingTier.Starter, 3)]
	[InlineData(PricingTier.Pro, int.MaxValue)]
	public void MaxWebhooks_CorrectPerTier(PricingTier tier, int expected)
	{
		TierPolicy.MaxWebhooks(tier).Should().Be(expected);
	}

	[Theory]
	[InlineData(PricingTier.Free, ChannelType.WhatsApp, true)]
	[InlineData(PricingTier.Free, ChannelType.Email, false)]
	[InlineData(PricingTier.Free, ChannelType.Sms, false)]
	[InlineData(PricingTier.Free, ChannelType.Webhook, false)]
	[InlineData(PricingTier.Starter, ChannelType.WhatsApp, true)]
	[InlineData(PricingTier.Starter, ChannelType.Email, true)]
	[InlineData(PricingTier.Starter, ChannelType.Sms, false)]
	[InlineData(PricingTier.Starter, ChannelType.Webhook, true)]
	[InlineData(PricingTier.Pro, ChannelType.WhatsApp, true)]
	[InlineData(PricingTier.Pro, ChannelType.Email, true)]
	[InlineData(PricingTier.Pro, ChannelType.Sms, true)]
	[InlineData(PricingTier.Pro, ChannelType.Webhook, true)]
	public void CanUseChannel_EnforcesTierBoundary(PricingTier tier, ChannelType ch, bool expected)
	{
		TierPolicy.CanUseChannel(tier, ch).Should().Be(expected);
	}

	[Fact]
	public void ValidAlertMinutes_ExactlyThreeValues()
	{
		TierPolicy.ValidAlertMinutes.Should().BeEquivalentTo([30, 60, 90]);
	}
}
