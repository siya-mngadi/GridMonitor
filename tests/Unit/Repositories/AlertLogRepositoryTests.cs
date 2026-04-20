using FluentAssertions;
using GridMonitor.Domain.Entities;
using GridMonitor.Domain.Enums;
using GridMonitor.Domain.Repositories;
using GridMonitor.Infrastructure.DataContext;
using GridMonitor.Infrastructure.Repositories;
using GridMonitor.Tests.Unit.Shared;

namespace GridMonitor.Tests.Unit.Repositories;

public class AlertLogRepositoryTests : IDisposable
{
	private readonly AppDbContext _db = InMemoryDb.Create();
	private readonly IAlertLogRepository _repo;
	public AlertLogRepositoryTests() => _repo = new AlertLogRepository(_db);
	public void Dispose() => _db.Dispose();

	private async Task<Guid> SeedSubscriptionIdAsync()
	{
		var p = await Seed.ProvinceAsync(_db);
		var m = await Seed.MunicipalityAsync(_db, p.Id);
		var s = await Seed.SuburbAsync(_db, m.Id);
		var u = await Seed.UserAsync(_db);
		var sub = await Seed.SubscriptionAsync(_db, u.Id, s.Id);
		return sub.Id;
	}

	[Fact]
	public async Task GetBySubscriptionAsync_ReturnsLogsNewestFirst()
	{
		var subId = await SeedSubscriptionIdAsync();
		var now = DateTime.UtcNow;
		await Seed.AlertLogAsync(_db, subId, sentAt: now.AddHours(-3));
		await Seed.AlertLogAsync(_db, subId, sentAt: now.AddHours(-1));
		await Seed.AlertLogAsync(_db, subId, sentAt: now.AddHours(-5));

		var result = await _repo.GetBySubscriptionAsync(subId, 10);

		result.Should().HaveCount(3);
		result[0].SentAt.Should().BeAfter(result[1].SentAt);
		result[1].SentAt.Should().BeAfter(result[2].SentAt);
	}

	[Fact]
	public async Task GetBySubscriptionAsync_RespectsLimit()
	{
		var subId = await SeedSubscriptionIdAsync();
		for (var i = 0; i < 10; i++)
			await Seed.AlertLogAsync(_db, subId);

		var result = await _repo.GetBySubscriptionAsync(subId, limit: 3);

		result.Should().HaveCount(3);
	}

	[Fact]
	public async Task GetBySubscriptionAsync_DifferentSubscription_ReturnsEmpty()
	{
		var subId = await SeedSubscriptionIdAsync();
		await Seed.AlertLogAsync(_db, subId);

		var result = await _repo.GetBySubscriptionAsync(Guid.NewGuid(), 10);

		result.Should().BeEmpty();
	}

	[Fact]
	public async Task WasAlertSentAsync_SuccessfulLogWithinWindow_ReturnsTrue()
	{
		var subId = await SeedSubscriptionIdAsync();
		await Seed.AlertLogAsync(_db, subId,
			stage: 3, evt: AlertEvent.StartingSoon,
			success: true, sentAt: DateTime.UtcNow.AddMinutes(-10));

		var result = await _repo.WasAlertSentAsync(
			subId, stage: 3, AlertEvent.StartingSoon, within: TimeSpan.FromHours(1));

		result.Should().BeTrue();
	}

	[Fact]
	public async Task WasAlertSentAsync_FailedLog_ReturnsFalse()
	{
		var subId = await SeedSubscriptionIdAsync();
		await Seed.AlertLogAsync(_db, subId,
			stage: 3, evt: AlertEvent.StartingSoon,
			success: false, sentAt: DateTime.UtcNow.AddMinutes(-10));

		var result = await _repo.WasAlertSentAsync(
			subId, stage: 3, AlertEvent.StartingSoon, within: TimeSpan.FromHours(1));

		result.Should().BeFalse("failed sends don't count as sent");
	}

	[Fact]
	public async Task WasAlertSentAsync_LogOutsideWindow_ReturnsFalse()
	{
		var subId = await SeedSubscriptionIdAsync();
		await Seed.AlertLogAsync(_db, subId,
			stage: 2, evt: AlertEvent.StartingSoon,
			success: true, sentAt: DateTime.UtcNow.AddHours(-25));

		var result = await _repo.WasAlertSentAsync(
			subId, stage: 2, AlertEvent.StartingSoon, within: TimeSpan.FromHours(24));

		result.Should().BeFalse("log is outside the dedup window");
	}

	[Fact]
	public async Task WasAlertSentAsync_DifferentStage_ReturnsFalse()
	{
		var subId = await SeedSubscriptionIdAsync();
		await Seed.AlertLogAsync(_db, subId,
			stage: 2, evt: AlertEvent.StartingSoon,
			success: true, sentAt: DateTime.UtcNow.AddMinutes(-5));

		var result = await _repo.WasAlertSentAsync(
			subId, stage: 4, AlertEvent.StartingSoon, within: TimeSpan.FromHours(1));

		result.Should().BeFalse("different stage = different alert");
	}

	[Fact]
	public async Task WasAlertSentAsync_DifferentEventType_ReturnsFalse()
	{
		var subId = await SeedSubscriptionIdAsync();
		await Seed.AlertLogAsync(_db, subId,
			stage: 3, evt: AlertEvent.StartingSoon,
			success: true, sentAt: DateTime.UtcNow.AddMinutes(-5));

		var result = await _repo.WasAlertSentAsync(
			subId, stage: 3, AlertEvent.Started, within: TimeSpan.FromHours(1));

		result.Should().BeFalse("different event type = different alert");
	}

	[Fact]
	public async Task PurgeOlderThanAsync_RemovesOldLogs_KeepsRecent()
	{
		var subId = await SeedSubscriptionIdAsync();
		var now = DateTime.UtcNow;
		await Seed.AlertLogAsync(_db, subId, sentAt: now.AddDays(-31)); // purged
		await Seed.AlertLogAsync(_db, subId, sentAt: now.AddDays(-29)); // purged
		await Seed.AlertLogAsync(_db, subId, sentAt: now.AddDays(-1));  // kept
		await Seed.AlertLogAsync(_db, subId, sentAt: now.AddHours(-2)); // kept

		await _repo.PurgeOlderThanAsync(TimeSpan.FromDays(30));
		await _repo.UnitOfWork.SaveEntitiesAsync();

		var remaining = await _repo.GetBySubscriptionAsync(subId, limit: 100);
		remaining.Should().HaveCount(2);
		remaining.Should().AllSatisfy(l => l.SentAt.Should().BeAfter(now.AddDays(-30)));
	}

	[Fact]
	public async Task AddAsync_ThenSave_PersistsLog()
	{
		var subId = await SeedSubscriptionIdAsync();
		var log = new AlertLog
		{
			SubscriptionId = subId,
			ChannelType = ChannelType.WhatsApp,
			Destination = "+27821234567",
			Event = AlertEvent.StartingSoon,
			Stage = 4,
			Success = true,
			AttemptCount = 1,
			SentAt = DateTime.UtcNow
		};

		await _repo.AddAsync(log);
		await _repo.UnitOfWork.SaveEntitiesAsync();

		(await _repo.GetBySubscriptionAsync(subId, 10)).Should().ContainSingle();
	}
}
