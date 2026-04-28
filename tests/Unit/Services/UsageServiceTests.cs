using FluentAssertions;
using GridMonitor.Application.Services;
using GridMonitor.Domain.Entities;
using GridMonitor.Domain.Enums;
using GridMonitor.Domain.Repositories;
using GridMonitor.Domain.Shared;
using GridMonitor.Tests.Unit.Shared;
using Microsoft.Extensions.Caching.Distributed;
using NSubstitute;

namespace GridMonitor.Tests.Unit.Services;

public class UsageServiceTests
{
	private readonly IApiKeyRepository _keys = Substitute.For<IApiKeyRepository>();
	private readonly IUserRepository _users = Substitute.For<IUserRepository>();
	private readonly IDistributedCache _cache = GenerateMockObjects.Cache();
	private readonly UsageService _service;

	public UsageServiceTests()
	{
		_service = new UsageService(_keys, _users, _cache);
	}

	// ── CheckAndIncrementAsync ────────────────────────────────────────────────

	[Fact]
	public async Task CheckAndIncrement_KeyNotFound_ReturnsFalse()
	{
		_keys.GetByIdAsync(Arg.Any<Guid>()).Returns(default(ApiKey));

		var result = await _service.CheckAndIncrementAsync(Guid.NewGuid());

		result.Value.Should().BeFalse();
	}

	[Fact]
	public async Task CheckAndIncrement_InactiveKey_ReturnsFalse()
	{
		var user = GenerateMockObjects.User();
		var key = GenerateMockObjects.ApiKey(user.Id, isActive: false);
		_keys.GetByIdAsync(key.Id).Returns(key);

		var result = await _service.CheckAndIncrementAsync(key.Id);

		result.Value.Should().BeFalse();
	}

	[Fact]
	public async Task CheckAndIncrement_ProTier_ReturnsTrueWithoutTouchingCache()
	{
		var user = GenerateMockObjects.User(tier: PricingTier.Pro);
		var key = GenerateMockObjects.ApiKey(user.Id, PricingTier.Pro); // DailyCallLimit = int.MaxValue
		_keys.GetByIdAsync(key.Id).Returns(key);

		var result = await _service.CheckAndIncrementAsync(key.Id);

		result.Value.Should().BeTrue();
		// Cache key should remain empty — pro skips Redis
		var cacheKey = $"usage:{key.Id}:{DateTime.UtcNow:yyyy-MM-dd}";
		(await _cache.GetStringAsync(cacheKey)).Should().BeNull();
	}

	[Fact]
	public async Task CheckAndIncrement_FirstCallOfDay_ReturnsTrueAndSetsCountToOne()
	{
		var user = GenerateMockObjects.User(tier: PricingTier.Free);
		var key = GenerateMockObjects.ApiKey(user.Id, PricingTier.Free); // limit = 100
		_keys.GetByIdAsync(key.Id).Returns(key);

		var result = await _service.CheckAndIncrementAsync(key.Id);

		result.Value.Should().BeTrue();
		var cacheKey = $"usage:{key.Id}:{DateTime.UtcNow:yyyy-MM-dd}";
		var stored = await _cache.GetStringAsync(cacheKey);
		stored.Should().Be("1");
	}

	[Fact]
	public async Task CheckAndIncrement_IncrementsProperly_OnSuccessiveCalls()
	{
		var user = GenerateMockObjects.User(tier: PricingTier.Free);
		var key = GenerateMockObjects.ApiKey(user.Id, PricingTier.Free);
		_keys.GetByIdAsync(key.Id).Returns(key);

		await _service.CheckAndIncrementAsync(key.Id);
		await _service.CheckAndIncrementAsync(key.Id);
		var third = await _service.CheckAndIncrementAsync(key.Id);

		third.Value.Should().BeTrue();
		var cacheKey = $"usage:{key.Id}:{DateTime.UtcNow:yyyy-MM-dd}";
		(await _cache.GetStringAsync(cacheKey)).Should().Be("3");
	}

	[Fact]
	public async Task CheckAndIncrement_AtLimit_ReturnsFalse_DoesNotIncrement()
	{
		var user = GenerateMockObjects.User(tier: PricingTier.Free);
		var key = GenerateMockObjects.ApiKey(user.Id, PricingTier.Free); // limit = 100
		_keys.GetByIdAsync(key.Id).Returns(key);

		// Pre-seed cache at the limit
		var cacheKey = $"usage:{key.Id}:{DateTime.UtcNow:yyyy-MM-dd}";
		await _cache.SetStringAsync(cacheKey, "100");

		var result = await _service.CheckAndIncrementAsync(key.Id);

		result.Value.Should().BeFalse();
		(await _cache.GetStringAsync(cacheKey)).Should().Be("100", because: "the counter must not increment past the limit");
	}

	[Fact]
	public async Task CheckAndIncrement_OneBelowLimit_ReturnsTrue()
	{
		var user = GenerateMockObjects.User(tier: PricingTier.Starter);
		var key = GenerateMockObjects.ApiKey(user.Id, PricingTier.Starter); // limit = 10_000
		_keys.GetByIdAsync(key.Id).Returns(key);

		var cacheKey = $"usage:{key.Id}:{DateTime.UtcNow:yyyy-MM-dd}";
		await _cache.SetStringAsync(cacheKey, "9999");

		var result = await _service.CheckAndIncrementAsync(key.Id);

		result.Value.Should().BeTrue();
		(await _cache.GetStringAsync(cacheKey)).Should().Be("10000");
	}

	[Fact]
	public async Task GetStats_NoActiveKey_ReturnsZeroCallsWithUserTier()
	{
		var user = GenerateMockObjects.User(tier: PricingTier.Starter);
		_users.GetByIdAsync(user.Id).Returns(user);
		_keys.GetApiKeysAsync(user.Id).Returns([]); // no keys

		var stats = await _service.GetStatsAsync(user.Id);

		stats.Value.TodayCalls.Should().Be(0);
		stats.Value.Tier.Should().Be(PricingTier.Starter);
	}

	[Fact]
	public async Task GetStats_WithUsage_ReflectsCacheCount()
	{
		var user = GenerateMockObjects.User(tier: PricingTier.Free);
		var key = GenerateMockObjects.ApiKey(user.Id, PricingTier.Free);

		_users.GetByIdAsync(user.Id).Returns(user);
		_keys.GetApiKeysAsync(user.Id).Returns([key]);
		// Simulate 42 calls already made today
		var cacheKey = $"usage:{key.Id}:{DateTime.UtcNow:yyyy-MM-dd}";
		await _cache.SetStringAsync(cacheKey, "42");

		var stats = await _service.GetStatsAsync(user.Id);

		stats.Value.TodayCalls.Should().Be(42);
		stats.Value.DailyLimit.Should().Be(50);
		stats.Value.Remaining.Should().Be(8);
		stats.Value.Tier.Should().Be(PricingTier.Free);
	}

	[Fact]
	public async Task GetStats_ProTier_RemainingIsMaxInt()
	{
		var user = GenerateMockObjects.User(tier: PricingTier.Pro);
		var key = GenerateMockObjects.ApiKey(user.Id, PricingTier.Pro);

		_users.GetByIdAsync(user.Id).Returns(user);
		_keys.GetApiKeysAsync(user.Id).Returns(new List<ApiKey> { key });
		var stats = await _service.GetStatsAsync(user.Id);

		stats.Value.Remaining.Should().Be(int.MaxValue);
		stats.Value.DailyLimit.Should().Be(int.MaxValue);
	}

	[Fact]
	public async Task GetStats_NoCacheEntry_TodayCallsIsZero()
	{
		var user = GenerateMockObjects.User(tier: PricingTier.Starter);
		var key = GenerateMockObjects.ApiKey(user.Id, PricingTier.Starter);

		_users.GetByIdAsync(user.Id).Returns(user);
		_keys.GetApiKeysAsync(user.Id).Returns(new List<ApiKey> { key });
		var stats = await _service.GetStatsAsync(user.Id);

		stats.Value.TodayCalls.Should().Be(0);
		stats.Value.Remaining.Should().Be(TierPolicy.DailyCallLimit(PricingTier.Starter));
	}

	[Fact]
	public async Task GetStats_MultipleKeys_UsesFirstActiveKey()
	{
		var user = GenerateMockObjects.User(tier: PricingTier.Free);
		var inactive = GenerateMockObjects.ApiKey(user.Id, PricingTier.Free, isActive: false);
		var active = GenerateMockObjects.ApiKey(user.Id, PricingTier.Free, isActive: true);

		_users.GetByIdAsync(user.Id).Returns(user);
		_keys.GetApiKeysAsync(user.Id).Returns(new List<ApiKey> { inactive, active });

		var cacheKey = $"usage:{active.Id}:{DateTime.UtcNow:yyyy-MM-dd}";
		await _cache.SetStringAsync(cacheKey, "10");

		var stats = await _service.GetStatsAsync(user.Id);

		stats.Value.TodayCalls.Should().Be(10, because: "inactive key's cache is not read");
	}

	[Fact]
	public async Task GetStats_RemainingNeverGoesNegative()
	{
		var user = GenerateMockObjects.User(tier: PricingTier.Free);
		var key = GenerateMockObjects.ApiKey(user.Id, PricingTier.Free); // limit = 100

		_users.GetByIdAsync(user.Id).Returns(user);
		_keys.GetApiKeysAsync(user.Id).Returns(new List<ApiKey> { key });
		// Seed more calls than the limit (edge case — shouldn't happen but guard it)
		var cacheKey = $"usage:{key.Id}:{DateTime.UtcNow:yyyy-MM-dd}";
		await _cache.SetStringAsync(cacheKey, "150");

		var stats = await _service.GetStatsAsync(user.Id);

		stats.Value.Remaining.Should().Be(0, because: "remaining is clamped to zero, never negative");
	}
}
