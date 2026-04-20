using FluentAssertions;
using GridMonitor.Domain.Entities;
using GridMonitor.Domain.Enums;
using GridMonitor.Domain.Repositories;
using GridMonitor.Infrastructure.DataContext;
using GridMonitor.Infrastructure.Repositories;
using GridMonitor.Tests.Unit.Shared;

namespace GridMonitor.Tests.Unit.Repositories;

public class SyncRunRepositoryTests : IDisposable
{
	private readonly AppDbContext _db = InMemoryDb.Create();
	private readonly ISyncRunRepository _repo;
	public SyncRunRepositoryTests() => _repo = new SyncRunRepository(_db);
	public void Dispose() => _db.Dispose();

	private async Task<SyncRun> SeedRunAsync(SyncEvent type = SyncEvent.FullSync,
		bool success = true, DateTime? startedAt = null)
	{
		var r = new SyncRun
		{
			Type = type,
			Success = success,
			StartedAt = startedAt ?? DateTime.UtcNow,
			FinishedAt = (startedAt ?? DateTime.UtcNow).AddMinutes(5)
		};
		_db.SyncRuns.Add(r);
		await _db.SaveChangesAsync();
		return r;
	}

	[Fact]
	public async Task GetLatestAsync_ReturnsNewestByType()
	{
		var now = DateTime.UtcNow;
		await SeedRunAsync(SyncEvent.FullSync, startedAt: now.AddHours(-10));
		await SeedRunAsync(SyncEvent.FullSync, startedAt: now.AddHours(-2));  // newest
		await SeedRunAsync(SyncEvent.StagePoll, startedAt: now.AddMinutes(-5));

		var result = await _repo.GetLatestAsync(SyncEvent.FullSync);

		result.Should().NotBeNull();
		result!.StartedAt.Should().BeCloseTo(now.AddHours(-2), TimeSpan.FromSeconds(1));
	}

	[Fact]
	public async Task GetLatestAsync_WrongType_ReturnsNull()
	{
		await SeedRunAsync(SyncEvent.FullSync);

		var result = await _repo.GetLatestAsync(SyncEvent.StagePoll);

		result.Should().BeNull();
	}

	[Fact]
	public async Task GetLatestAsync_NoRuns_ReturnsNull()
	{
		var result = await _repo.GetLatestAsync(SyncEvent.FullSync);
		result.Should().BeNull();
	}

	[Fact]
	public async Task GetRecentAsync_ReturnsNewestFirst_RespectsLimit()
	{
		var now = DateTime.UtcNow;
		for (var i = 0; i < 15; i++)
			await SeedRunAsync(startedAt: now.AddMinutes(-i));

		var result = await _repo.GetRecentAsync(limit: 5);

		result.Should().HaveCount(5);
		result[0].StartedAt.Should().BeAfter(result[1].StartedAt);
	}

	[Fact]
	public async Task AddAsync_ThenSave_PersistsRun()
	{
		var run = new SyncRun
		{
			Type = SyncEvent.FullSync,
			Success = true,
			MunicipalitiesProcessed = 213,
			SuburbProcessed = 4800,
			StartedAt = DateTime.UtcNow,
			FinishedAt = DateTime.UtcNow.AddMinutes(12)
		};

		await _repo.AddAsync(run);
		await _repo.UnitOfWork.SaveEntitiesAsync();

		var latest = await _repo.GetLatestAsync(SyncEvent.FullSync);
		latest.Should().NotBeNull();
		latest!.SuburbProcessed.Should().Be(4800);
		latest.FinishedAt.Should().NotBeNull();
	}

	[Fact]
	public async Task GetRecentAsync_MixedTypes_ReturnedTogetherNewestFirst()
	{
		var now = DateTime.UtcNow;
		await SeedRunAsync(SyncEvent.FullSync, startedAt: now.AddHours(-3));
		await SeedRunAsync(SyncEvent.StagePoll, startedAt: now.AddHours(-1));
		await SeedRunAsync(SyncEvent.FullSync, startedAt: now.AddHours(-2));

		var result = await _repo.GetRecentAsync(limit: 10);

		result.Should().HaveCount(3);
		result[0].StartedAt.Should().BeAfter(result[1].StartedAt);
		result[1].StartedAt.Should().BeAfter(result[2].StartedAt);
	}
}
