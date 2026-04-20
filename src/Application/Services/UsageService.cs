using GridMonitor.Domain.Enums;
using GridMonitor.Domain.Repositories;
using GridMonitor.Domain.Services;
using GridMonitor.Domain.Shared;
using GridMonitor.Domain.ValueObjects;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

namespace GridMonitor.Application.Services;

public class UsageService : IUsageService
{
	private readonly IApiKeyRepository apiKeyRepository;
	private readonly IUserRepository userRepository;
	private readonly IDistributedCache cache;

	public UsageService(
		IApiKeyRepository apiKeyRepository, 
		IUserRepository userRepository,
		IDistributedCache cache)
	{
		this.apiKeyRepository = apiKeyRepository;
		this.userRepository = userRepository;
		this.cache = cache;
	}

	public async ValueTask<Result<bool>> CheckAndIncrementAsync(Guid apiKeyId, CancellationToken ct = default)
	{
		var key = await apiKeyRepository.GetByIdAsync(apiKeyId, ct);
		if (key is null || !key.Active) return Result<bool>.Fail("API key is invalid or inactive");

		// Pro tier has no limit — skip Redis entirely
		if (key.DailyCallLimit == int.MaxValue) return Result<bool>.Ok(true);
		var cacheKey = $"usage:{apiKeyId}:{DateTime.UtcNow:yyyy-MM-dd}";

		var raw = await cache.GetStringAsync(cacheKey, ct);
		var count = raw is null ? 0 : int.Parse(raw);

		if (count >= key.DailyCallLimit) return Result<bool>.Ok(false);

		// Increment and set TTL to end of day
		var newCount = count + 1;
		var midnight = DateTime.UtcNow.Date.AddDays(1);
		var ttl = midnight - DateTime.UtcNow;

		await cache.SetStringAsync(cacheKey, newCount.ToString(),
			new DistributedCacheEntryOptions
			{
				AbsoluteExpirationRelativeToNow = ttl,
			}, ct);

		return Result<bool>.Ok(true);
	}

	public async ValueTask<Result<UsageStatsResult>> GetStatsAsync(Guid userId, CancellationToken ct = default)
	{
		var user = await userRepository.GetByIdAsync(userId, ct);
		if (user is null) return Result<UsageStatsResult>.Ok(new UsageStatsResult(0, 0, 0, PricingTier.Free));

		var keys = await apiKeyRepository.GetApiKeysAsync(userId, ct);
		var activeKey = keys.FirstOrDefault(k => k.Active);
		if (activeKey is null) return Result<UsageStatsResult>.Ok(new UsageStatsResult(0, 0, 0, user.Tier));

		var cacheKey = $"usage:{activeKey.Id}:{DateTime.UtcNow:yyyy-MM-dd}";
		var raw = await cache.GetStringAsync(cacheKey, ct);
		var today = raw is null ? 0 : int.Parse(raw);
		var limit = activeKey.DailyCallLimit;
		var remaining = limit == int.MaxValue ? int.MaxValue : Math.Max(0, limit - today);

		return Result<UsageStatsResult>.Ok(new UsageStatsResult(today, limit, remaining, user.Tier));
	}
}
