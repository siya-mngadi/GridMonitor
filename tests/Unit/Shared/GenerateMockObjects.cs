using GridMonitor.Application.Helpers;
using GridMonitor.Domain.Entities;
using GridMonitor.Domain.Enums;
using GridMonitor.Domain.Shared;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GridMonitor.Tests.Unit.Shared;

public class GenerateMockObjects
{
	public static User User(
		string email = "test@test.com",
		PricingTier tier = PricingTier.Free,
		bool isActive = true)
	{
		return new()
		{
			Id = Guid.NewGuid(),
			Email = email,
			Password = "hash",
			Tier = tier,
			Active = isActive,
			CreatedAt = DateTime.UtcNow
		};
	}

	public static ApiKey ApiKey(
		Guid userId,
		PricingTier tier = PricingTier.Free,
		bool isActive = true)
	{
		return new()
		{
			Id = Guid.NewGuid(),
			UserId = userId,
			KeyHash = ApiKeyHelper.Hash($"ls_test_{Guid.NewGuid():N}"),
			KeyPrefix = "ls_test...",
			Active = isActive,
			DailyCallLimit = TierPolicy.DailyCallLimit(tier),
			CreatedAt = DateTime.UtcNow
		};
	}

	public static Suburb Suburb(int id = 1) => new()
	{
		Id = id,
		EskomId = id,
		Name = $"Suburb {id}",
		MunicipalityId = 1,
		Total = 3,
		LastSyncedAt = DateTime.UtcNow
	};

	public static AlertSubscription Subscription(
		Guid userId,
		int suburbId,
		bool isActive = true,
		short minutesBefore = 30)
	{
		return new()
		{
			Id = Guid.NewGuid(),
			UserId = userId,
			SuburbId = suburbId,
			AlertMinutesBefore = minutesBefore,
			Active = isActive,
			CreatedAt = DateTime.UtcNow
		};
	}

	public static AlertChannel Channel(
		Guid subscriptionId,
		ChannelType type = ChannelType.WhatsApp,
		string destination = "+27821234567",
		bool isActive = true)
	{
		return new()
		{
			Id = Guid.NewGuid(),
			SubscriptionId = subscriptionId,
			ChannelType = type,
			Destination = destination,
			Active = isActive
		};
	}

	public static ScheduleSlot Slot(
		int suburbId,
		short stage = 2,
		DayOfWeek dayNumber = DayOfWeek.Monday,
		TimeOnly? start = null,
		TimeOnly? end = null)
	{
		return new()
		{
			SuburbId = suburbId,
			Stage = stage,
			ScheduleDay = dayNumber,
			StartTime = start ?? new TimeOnly(22, 0),
			EndTime = end ?? new TimeOnly(0, 30),
			DataHash = "hash"
		};
	}

	/// <summary>Real MemoryDistributedCache — exercises actual cache paths without Redis.</summary>
	public static IDistributedCache Cache()
	{
		return new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
	}
}
