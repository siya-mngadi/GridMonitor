using GridMonitor.Application.Helpers;
using GridMonitor.Domain.Entities;
using GridMonitor.Domain.Enums;
using GridMonitor.Infrastructure.DataContext;

namespace GridMonitor.Tests.Unit.Shared;

public static class Seed
{
	public static async Task<Province> ProvinceAsync(AppDbContext db, int eskomId = 1)
	{
		var p = new Province { EskomId = eskomId, Name = $"Province-{eskomId}", LastSyncedAt = DateTime.MinValue };
		db.Provinces.Add(p);
		await db.SaveChangesAsync();
		return p;
	}

	public static async Task<Municipality> MunicipalityAsync(AppDbContext db, int provinceId)
	{
		var m = new Municipality
		{
			EskomId = Random.Shared.Next(1000_000, 99_999_999),
			Name = $"Muni-{Guid.NewGuid():N}",
			ProvinceId = provinceId,
			Total = 10,
			LastSyncedAt = DateTime.MinValue
		};
		db.Municipalities.Add(m);
		await db.SaveChangesAsync();
		return m;
	}

	public static async Task<Suburb> SuburbAsync(AppDbContext db, int municipalityId, string name = null)
	{
		var s = new Suburb
		{
			EskomId = Random.Shared.Next(10_000, 99_999),
			Name = name ?? $"Suburb-{Guid.NewGuid():N}",
			MunicipalityId = municipalityId,
			Total = 3,
			LastSyncedAt = DateTime.UtcNow
		};
		db.Suburbs.Add(s);
		await db.SaveChangesAsync();
		return s;
	}

	public static async Task<ApiKey> ApiKeyAsync(
		AppDbContext db,
		Guid userId,
	   bool isActive = true,
	   PricingTier tier = PricingTier.Free)
	{
		var k = new ApiKey
		{
			Id = Guid.NewGuid(),
			UserId = userId,
			KeyHash = ApiKeyHelper.Hash($"ls_test_{Guid.NewGuid():N}"),
			KeyPrefix = "ls_test...",
			Active = isActive,
			DailyCallLimit = tier == PricingTier.Pro ? int.MaxValue : tier == PricingTier.Starter ? 10_000 : 100,
			CreatedAt = DateTime.UtcNow
		};
		db.ApiKeys.Add(k);
		await db.SaveChangesAsync();
		return k;
	}

	public static async Task<AlertSubscription> SubscriptionAsync(
		AppDbContext db,
		Guid userId,
		int suburbId,
		bool isActive = true,
		short minutesBefore = 30)
	{
		var s = new AlertSubscription
		{
			Id = Guid.NewGuid(),
			UserId = userId,
			SuburbId = suburbId,
			AlertMinutesBefore = minutesBefore,
			Active = isActive,
			CreatedAt = DateTime.UtcNow
		};
		db.Subscriptions.Add(s);
		await db.SaveChangesAsync();
		return s;
	}


	public static async Task<User> UserAsync(AppDbContext db, string email = null, PricingTier tier = PricingTier.Free)
	{
		var u = new User
		{
			Id = Guid.NewGuid(),
			Email = email ?? $"user-{Guid.NewGuid():N}@test.com",
			Password = "hash",
			Tier = tier,
			Active = true,
			CreatedAt = DateTime.UtcNow
		};
		db.Users.Add(u);
		await db.SaveChangesAsync();
		return u;
	}

	public static async Task<AlertChannel> ChannelAsync(
	   AppDbContext db,
	   Guid subscriptionId,
	   ChannelType type = ChannelType.WhatsApp,
	   string destination = "+27821234567",
	   bool isActive = true)
	{
		var c = new AlertChannel
		{
			Id = Guid.NewGuid(),
			SubscriptionId = subscriptionId,
			ChannelType = type,
			Destination = destination,
			Active = isActive
		};
		db.Channels.Add(c);
		await db.SaveChangesAsync();
		return c;
	}

	public static async Task<AlertLog> AlertLogAsync(
		AppDbContext db, Guid subscriptionId,
		short stage = 2,
		AlertEvent evt = AlertEvent.StartingSoon,
		bool success = true,
		DateTime? sentAt = null)
	{
		var l = new AlertLog
		{
			SubscriptionId = subscriptionId,
			ChannelType = ChannelType.WhatsApp,
			Destination = "+27821234567",
			Event = evt,
			Stage = stage,
			Success = success,
			AttemptCount = 1,
			SentAt = sentAt ?? DateTime.UtcNow
		};
		db.AlertLogs.Add(l);
		await db.SaveChangesAsync();
		return l;
	}

	public static async Task<StageSnapshot> SnapshotAsync(
		AppDbContext db,
		short stage,
		DateTime? recordedAt = null)
	{
		var s = new StageSnapshot
		{
			Stage = stage,
			RawText = (stage + 1).ToString(),
			CreatedAt = recordedAt ?? DateTime.UtcNow
		};
		db.StageSnapshots.Add(s);
		await db.SaveChangesAsync();
		return s;
	}
}
