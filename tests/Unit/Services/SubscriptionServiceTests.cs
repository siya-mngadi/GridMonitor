using FluentAssertions;
using GridMonitor.Application.Services;
using GridMonitor.Domain.Entities;
using GridMonitor.Domain.Enums;
using GridMonitor.Domain.Repositories;
using GridMonitor.Tests.Unit.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace GridMonitor.Tests.Unit.Services;

public class SubscriptionServiceTests
{
	private readonly IAlertSubscriptionRepository _subs = Substitute.For<IAlertSubscriptionRepository>();
	private readonly IAlertChannelRepository _chans = Substitute.For<IAlertChannelRepository>();
	private readonly ISuburbRepository _suburbs = Substitute.For<ISuburbRepository>();
	private readonly IUserRepository _users = Substitute.For<IUserRepository>();
	private readonly SubscriptionService _service;

	public SubscriptionServiceTests()
	{
		_service = new SubscriptionService(_subs, _chans, _suburbs, _users, NullLogger<SubscriptionService>.Instance);
	}

	[Theory]
	[InlineData(30)]
	[InlineData(60)]
	[InlineData(90)]
	public async Task Subscribe_ValidAlertWindow_Succeeds(short minutes)
	{
		var user = GenerateMockObjects.User();
		_suburbs.GetByIdAsync(1).Returns(GenerateMockObjects.Suburb(1));
		_users.GetByIdAsync(user.Id).Returns(user);
		_subs.GetByUserAndSuburbAsync(user.Id, 1).Returns(default(AlertSubscription));

		var result = await _service.SubscribeAsync(user.Id, 1, minutes);

		result.Success.Should().BeTrue();
		result.Value!.AlertMinutesBefore.Should().Be(minutes);
		result.Value.Active.Should().BeTrue();
	}

	[Theory]
	[InlineData(0)]
	[InlineData(10)]
	[InlineData(20)]
	[InlineData(45)]
	[InlineData(61)]
	[InlineData(-1)]
	public async Task Subscribe_InvalidAlertWindow_Fails_BeforeAnyDbCall(short minutes)
	{
		var result = await _service.SubscribeAsync(Guid.NewGuid(), 1, minutes);

		result.Success.Should().BeFalse();
		result.Error.Should().Contain("30, 60, or 90");
		await _suburbs.DidNotReceive().GetByIdAsync(Arg.Any<int>());
	}

	[Fact]
	public async Task Subscribe_SuburbNotFound_Fails()
	{
		_suburbs.GetByIdAsync(Arg.Any<int>()).Returns(default(Suburb));

		var result = await _service.SubscribeAsync(Guid.NewGuid(), 99, 30);

		result.Success.Should().BeFalse();
		result.Error.Should().Contain("Suburb not found");
	}

	[Fact]
	public async Task Subscribe_UserNotFound_Fails()
	{
		_suburbs.GetByIdAsync(1).Returns(GenerateMockObjects.Suburb());
		_users.GetByIdAsync(Arg.Any<Guid>()).Returns(default(User));

		var result = await _service.SubscribeAsync(Guid.NewGuid(), 1, 30);

		result.Success.Should().BeFalse();
		result.Error.Should().Contain("User not found");
	}

	[Fact]
	public async Task Subscribe_DeactivatedUser_Fails()
	{
		var user = GenerateMockObjects.User(isActive: false);
		_suburbs.GetByIdAsync(1).Returns(GenerateMockObjects.Suburb());
		_users.GetByIdAsync(user.Id).Returns(user);

		var result = await _service.SubscribeAsync(user.Id, 1, 30);

		result.Success.Should().BeFalse();
		result.Error.Should().Contain("deactivated");
	}

	[Fact]
	public async Task Subscribe_ActiveDuplicate_Fails()
	{
		var user = GenerateMockObjects.User();
		var existing = GenerateMockObjects.Subscription(user.Id, 1, isActive: true);

		_suburbs.GetByIdAsync(1).Returns(GenerateMockObjects.Suburb());
		_users.GetByIdAsync(user.Id).Returns(user);
		_subs.GetByUserAndSuburbAsync(user.Id, 1).Returns(existing);

		var result = await _service.SubscribeAsync(user.Id, 1, 30);

		result.Success.Should().BeFalse();
		result.Error.Should().Contain("already have an active subscription");
		await _subs.DidNotReceive().AddAsync(Arg.Any<AlertSubscription>());
	}

	[Fact]
	public async Task Subscribe_PreviouslySoftDeleted_ReactivatesExisting_DoesNotInsertNew()
	{
		var user = GenerateMockObjects.User();
		var inactive = GenerateMockObjects.Subscription(user.Id, 1, isActive: false, minutesBefore: 15);

		_suburbs.GetByIdAsync(1).Returns(GenerateMockObjects.Suburb());
		_users.GetByIdAsync(user.Id).Returns(user);
		_subs.GetByUserAndSuburbAsync(user.Id, 1).Returns(inactive);

		var result = await _service.SubscribeAsync(user.Id, 1, 60);

		result.Success.Should().BeTrue();
		inactive.Active.Should().BeTrue();
		inactive.AlertMinutesBefore.Should().Be(60, because: "window is updated on reactivation");
		await _subs.DidNotReceive().AddAsync(Arg.Any<AlertSubscription>());
		await _subs.Received(1).UnitOfWork.SaveEntitiesAsync();
	}

	[Fact]
	public async Task Subscribe_NewSubscription_AddsAndSaves()
	{
		var user = GenerateMockObjects.User();
		_suburbs.GetByIdAsync(1).Returns(GenerateMockObjects.Suburb());
		_users.GetByIdAsync(user.Id).Returns(user);
		_subs.GetByUserAndSuburbAsync(user.Id, 1).Returns(default(AlertSubscription));

		await _service.SubscribeAsync(user.Id, 1, 30);

		await _subs.Received(1).AddAsync(Arg.Is<AlertSubscription>(s =>
			s.UserId == user.Id && s.SuburbId == 1 && s.AlertMinutesBefore == 30));
		await _subs.Received(1).UnitOfWork.SaveEntitiesAsync();
	}

	// ── UnsubscribeAsync ──────────────────────────────────────────────────────

	[Fact]
	public async Task Unsubscribe_OwnActiveSub_SoftDeletes()
	{
		var user = GenerateMockObjects.User();
		var sub = GenerateMockObjects.Subscription(user.Id, 1, isActive: true);
		_subs.GetByIdAsync(sub.Id).Returns(sub);

		var result = await _service.UnsubscribeAsync(user.Id, sub.Id);

		result.Success.Should().BeTrue();
		sub.Active.Should().BeFalse("unsubscribe is a soft delete");
		await _subs.Received(1).UnitOfWork.SaveEntitiesAsync();
	}

	[Fact]
	public async Task Unsubscribe_SubscriptionNotFound_Fails()
	{
		_subs.GetByIdAsync(Arg.Any<Guid>()).Returns(default(AlertSubscription));

		var result = await _service.UnsubscribeAsync(Guid.NewGuid(), Guid.NewGuid());

		result.Success.Should().BeFalse();
		result.Error.Should().Contain("not found");
	}

	[Fact]
	public async Task Unsubscribe_OtherUsersSub_Fails()
	{
		var sub = GenerateMockObjects.Subscription(Guid.NewGuid(), 1);
		_subs.GetByIdAsync(sub.Id).Returns(sub);

		var result = await _service.UnsubscribeAsync(Guid.NewGuid(), sub.Id);

		result.Success.Should().BeFalse();
		result.Error.Should().Contain("denied");
		sub.Active.Should().BeTrue("sub must not be mutated");
	}

	[Fact]
	public async Task Unsubscribe_AlreadyInactive_Fails()
	{
		var user = GenerateMockObjects.User();
		var sub = GenerateMockObjects.Subscription(user.Id, 1, isActive: false);
		_subs.GetByIdAsync(sub.Id).Returns(sub);

		var result = await _service.UnsubscribeAsync(user.Id, sub.Id);

		result.Success.Should().BeFalse();
		result.Error.Should().Contain("already inactive");
	}

	private void SetupAddChannel(
		User user, 
		AlertSubscription sub,
		List<AlertChannel> existingChannels = null)
	{
		_users.GetByIdAsync(user.Id).Returns(user);
		_subs.GetByIdAsync(sub.Id).Returns(sub);
		_chans.GetBySubscriptionAsync(sub.Id)
			  .Returns(existingChannels ?? new List<AlertChannel>());
	}

	[Theory]
	[InlineData(PricingTier.Free, ChannelType.Email, false)]
	[InlineData(PricingTier.Free, ChannelType.Sms, false)]
	[InlineData(PricingTier.Free, ChannelType.Webhook, false)]
	[InlineData(PricingTier.Starter, ChannelType.Email, true)]
	[InlineData(PricingTier.Starter, ChannelType.Webhook, true)]
	[InlineData(PricingTier.Starter, ChannelType.Sms, false)]
	[InlineData(PricingTier.Pro, ChannelType.Sms, true)]
	[InlineData(PricingTier.Pro, ChannelType.Webhook, true)]
	public async Task AddChannel_TierGating_EnforcedBeforeAdd(
		PricingTier tier,
		ChannelType channel,
		bool shouldSucceed)
	{
		var user = GenerateMockObjects.User(tier: tier);
		var sub = GenerateMockObjects.Subscription(user.Id, 1);
		SetupAddChannel(user, sub);

		var dest = channel switch
		{
			ChannelType.Email => "t@test.com",
			ChannelType.Sms => "+27821234567",
			ChannelType.Webhook => "https://hook.test.com",
			_ => "+27821234567"
		};

		var result = await _service.AddChannelAsync(user.Id, sub.Id, channel, dest);

		result.Success.Should().Be(shouldSucceed);
		if (!shouldSucceed)
			result.Error.Should().Contain("higher plan");
	}

	[Theory]
	[InlineData(ChannelType.WhatsApp, "0821234567", true)]
	[InlineData(ChannelType.WhatsApp, "+27821234567", true)]
	[InlineData(ChannelType.WhatsApp, "27821234567", true)]
	[InlineData(ChannelType.WhatsApp, "not-a-phone", false)]
	[InlineData(ChannelType.WhatsApp, "123456", false)]
	[InlineData(ChannelType.Sms, "0829876543", true)]
	[InlineData(ChannelType.Sms, "+27829876543", true)]
	[InlineData(ChannelType.Sms, "bad", false)]
	[InlineData(ChannelType.Email, "user@example.com", true)]
	[InlineData(ChannelType.Email, "user@sub.example.com", true)]
	[InlineData(ChannelType.Email, "no-at-sign", false)]
	[InlineData(ChannelType.Email, "no-dot@domain", false)]
	[InlineData(ChannelType.Webhook, "https://example.com", true)]
	[InlineData(ChannelType.Webhook, "http://example.com", true)]
	[InlineData(ChannelType.Webhook, "ftp://example.com", false)]
	[InlineData(ChannelType.Webhook, "not-a-url", false)]
	[InlineData(ChannelType.Webhook, "", false)]
	public async Task AddChannel_DestinationValidation(
		ChannelType channel, string destination, bool shouldSucceed)
	{
		var user = GenerateMockObjects.User(tier: PricingTier.Pro); // pro avoids tier gate
		var sub = GenerateMockObjects.Subscription(user.Id, 1);
		SetupAddChannel(user, sub);

		var result = await _service.AddChannelAsync(user.Id, sub.Id, channel, destination);

		result.Success.Should().Be(shouldSucceed,
			because: $"'{destination}' for {channel} should {(shouldSucceed ? "pass" : "fail")}");
	}

	[Fact]
	public async Task AddChannel_WhatsApp_FreeUser_Succeeds()
	{
		var user = GenerateMockObjects.User(tier: PricingTier.Free);
		var sub = GenerateMockObjects.Subscription(user.Id, 1);
		SetupAddChannel(user, sub);

		var result = await _service.AddChannelAsync(user.Id, sub.Id, ChannelType.WhatsApp, "0821234567");

		result.Success.Should().BeTrue();
	}

	[Fact]
	public async Task AddChannel_Webhook_GeneratesHmacSecret()
	{
		var user = GenerateMockObjects.User(tier: PricingTier.Pro);
		var sub = GenerateMockObjects.Subscription(user.Id, 1);
		SetupAddChannel(user, sub);

		var result = await _service.AddChannelAsync(
			user.Id, sub.Id, ChannelType.Webhook, "https://hook.example.com");

		result.Success.Should().BeTrue();
		result.Value!.WebhookSecret.Should().NotBeNullOrWhiteSpace();
		result.Value.WebhookSecret!.Length.Should().Be(64,
			because: "32 random bytes rendered as hex = 64 characters");
	}

	[Fact]
	public async Task AddChannel_NonWebhook_DoesNotGenerateSecret()
	{
		var user = GenerateMockObjects.User(tier: PricingTier.Free);
		var sub = GenerateMockObjects.Subscription(user.Id, 1);
		SetupAddChannel(user, sub);

		var result = await _service.AddChannelAsync(
			user.Id, sub.Id, ChannelType.WhatsApp, "+27821234567");

		result.Value!.WebhookSecret.Should().BeNull();
	}

	[Fact]
	public async Task AddChannel_Duplicate_SameTypeAndDestination_Fails()
	{
		var user = GenerateMockObjects.User(tier: PricingTier.Free);
		var sub = GenerateMockObjects.Subscription(user.Id, 1);
		var existing = GenerateMockObjects.Channel(sub.Id, ChannelType.WhatsApp, "+27821234567");
		SetupAddChannel(user, sub, [existing]);

		var result = await _service.AddChannelAsync(
			user.Id, sub.Id, ChannelType.WhatsApp, "+27821234567");

		result.Success.Should().BeFalse();
		result.Error.Should().Contain("already exists");
	}

	[Fact]
	public async Task AddChannel_SameDestination_DifferentType_Allowed()
	{
		var user = GenerateMockObjects.User(tier: PricingTier.Pro);
		var sub = GenerateMockObjects.Subscription(user.Id, 1);
		var existing = GenerateMockObjects.Channel(sub.Id, ChannelType.WhatsApp, "+27821234567");
		SetupAddChannel(user, sub, new List<AlertChannel> { existing });

		// SMS to the same number is a different channel type — should be allowed
		var result = await _service.AddChannelAsync(
			user.Id, sub.Id, ChannelType.Sms, "+27821234567");

		result.Success.Should().BeTrue();
	}

	[Fact]
	public async Task AddChannel_SubscriptionNotFound_Fails()
	{
		var user = GenerateMockObjects.User(tier: PricingTier.Pro);
		_users.GetByIdAsync(user.Id).Returns(user);
		_subs.GetByIdAsync(Arg.Any<Guid>()).Returns(default(AlertSubscription));

		var result = await _service.AddChannelAsync(
			user.Id, Guid.NewGuid(), ChannelType.WhatsApp, "+27821234567");

		result.Success.Should().BeFalse();
		result.Error.Should().Contain("not found");
	}

	[Fact]
	public async Task AddChannel_OtherUsersSub_Fails()
	{
		var user = GenerateMockObjects.User(tier: PricingTier.Pro);
		var sub = GenerateMockObjects.Subscription(Guid.NewGuid(), 1); // different owner
		_users.GetByIdAsync(user.Id).Returns(user);
		_subs.GetByIdAsync(sub.Id).Returns(sub);

		var result = await _service.AddChannelAsync(
			user.Id, sub.Id, ChannelType.WhatsApp, "+27821234567");

		result.Success.Should().BeFalse();
		result.Error.Should().Contain("denied");
	}

	[Fact]
	public async Task AddChannel_InactiveSub_Fails()
	{
		var user = GenerateMockObjects.User(tier: PricingTier.Free);
		var sub = GenerateMockObjects.Subscription(user.Id, 1, isActive: false);
		_users.GetByIdAsync(user.Id).Returns(user);
		_subs.GetByIdAsync(sub.Id).Returns(sub);

		var result = await _service.AddChannelAsync(
			user.Id, sub.Id, ChannelType.WhatsApp, "+27821234567");

		result.Success.Should().BeFalse();
		result.Error.Should().Contain("not active");
	}

	[Fact]
	public async Task AddChannel_TrimsDestinationWhitespace()
	{
		var user = GenerateMockObjects.User(tier: PricingTier.Free);
		var sub = GenerateMockObjects.Subscription(user.Id, 1);
		SetupAddChannel(user, sub);

		var result = await _service.AddChannelAsync(
			user.Id, sub.Id, ChannelType.WhatsApp, "  +27821234567  ");

		result.Success.Should().BeTrue();
		result.Value!.Destination.Should().Be("+27821234567");
	}

	[Fact]
	public async Task RemoveChannel_OwnChannel_Deactivates()
	{
		var user = GenerateMockObjects.User();
		var sub = GenerateMockObjects.Subscription(user.Id, 1);
		var channel = GenerateMockObjects.Channel(sub.Id);

		_chans.GetByIdAsync(channel.Id).Returns(channel);
		_subs.GetByIdAsync(sub.Id).Returns(sub);

		var result = await _service.RemoveChannelAsync(user.Id, channel.Id);

		result.Success.Should().BeTrue();
		await _chans.Received(1).DeactivateAsync(channel.Id);
		await _chans.Received(1).UnitOfWork.SaveEntitiesAsync();
	}

	[Fact]
	public async Task RemoveChannel_ChannelNotFound_Fails()
	{
		_chans.GetByIdAsync(Arg.Any<Guid>()).Returns(default(AlertChannel));

		var result = await _service.RemoveChannelAsync(Guid.NewGuid(), Guid.NewGuid());

		result.Success.Should().BeFalse();
		result.Error.Should().Contain("not found");
	}

	[Fact]
	public async Task RemoveChannel_OtherUsersSub_Fails()
	{
		var channel = GenerateMockObjects.Channel(Guid.NewGuid());
		var sub = GenerateMockObjects.Subscription(Guid.NewGuid(), 1); // different owner

		_chans.GetByIdAsync(channel.Id).Returns(channel);
		_subs.GetByIdAsync(channel.SubscriptionId).Returns(sub);

		var result = await _service.RemoveChannelAsync(Guid.NewGuid(), channel.Id);

		result.Success.Should().BeFalse();
		result.Error.Should().Contain("denied");
		await _chans.DidNotReceive().DeactivateAsync(Arg.Any<Guid>());
	}

	[Theory]
	[InlineData(30)]
	[InlineData(60)]
	[InlineData(90)]
	public async Task UpdateAlertWindow_ValidMinutes_Updates(short minutes)
	{
		var user = GenerateMockObjects.User();
		var sub = GenerateMockObjects.Subscription(user.Id, 1, minutesBefore: 30);
		_subs.GetByIdAsync(sub.Id).Returns(sub);

		var result = await _service.UpdateAlertWindowAsync(user.Id, sub.Id, minutes);

		result.Success.Should().BeTrue();
		sub.AlertMinutesBefore.Should().Be(minutes);
		await _subs.Received(1).UnitOfWork.SaveEntitiesAsync();
	}

	[Theory]
	[InlineData(0)]
	[InlineData(5)]
	[InlineData(45)]
	[InlineData(120)]
	public async Task UpdateAlertWindow_InvalidMinutes_Fails_BeforeDbCall(short minutes)
	{
		var result = await _service.UpdateAlertWindowAsync(Guid.NewGuid(), Guid.NewGuid(), minutes);

		result.Success.Should().BeFalse();
		result.Error.Should().Contain("15, 30, or 60");
		await _subs.DidNotReceive().GetByIdAsync(Arg.Any<Guid>());
	}

	[Fact]
	public async Task UpdateAlertWindow_SubNotFound_Fails()
	{
		_subs.GetByIdAsync(Arg.Any<Guid>()).Returns(default(AlertSubscription));

		var result = await _service.UpdateAlertWindowAsync(Guid.NewGuid(), Guid.NewGuid(), 30);

		result.Success.Should().BeFalse();
		result.Error.Should().Contain("not found");
	}

	[Fact]
	public async Task UpdateAlertWindow_OtherUsersSub_Fails()
	{
		var sub = GenerateMockObjects.Subscription(Guid.NewGuid(), 1);
		_subs.GetByIdAsync(sub.Id).Returns(sub);

		var result = await _service.UpdateAlertWindowAsync(Guid.NewGuid(), sub.Id, 30);

		result.Success.Should().BeFalse();
		result.Error.Should().Contain("denied");
		sub.AlertMinutesBefore.Should().Be(30, "value must not be mutated");
	}

	[Fact]
	public async Task UpdateAlertWindow_InactiveSub_Fails()
	{
		var user = GenerateMockObjects.User();
		var sub = GenerateMockObjects.Subscription(user.Id, 1, isActive: false);
		_subs.GetByIdAsync(sub.Id).Returns(sub);

		var result = await _service.UpdateAlertWindowAsync(user.Id, sub.Id, 60);

		result.Success.Should().BeFalse();
		result.Error.Should().Contain("not active");
	}

	[Fact]
	public async Task GetUserSubscriptions_DelegatesToRepository()
	{
		var userId = Guid.NewGuid();
		var list = new List<AlertSubscription> { GenerateMockObjects.Subscription(userId, 1) };
		_subs.GetByUserAsync(userId).Returns(list);

		var result = await _service.GetUserSubscriptionsAsync(userId);

		result.Value.Should().BeEquivalentTo(list);
		await _subs.Received(1).GetByUserAsync(userId);
	}
}
