using FluentAssertions;
using GridMonitor.Domain.Entities;
using GridMonitor.Domain.Repositories;
using GridMonitor.Infrastructure.DataContext;
using GridMonitor.Infrastructure.Repositories;
using GridMonitor.Tests.Unit.Shared;

namespace GridMonitor.Tests.Unit.Repositories;

public class StageSnapshotRepositoryTests : IDisposable
{
	private readonly AppDbContext _db = InMemoryDb.Create();
	private readonly IStageSnapshotRepository _repo;
	public StageSnapshotRepositoryTests() => _repo = new StageSnapshotRepository(_db);
	public void Dispose() => _db.Dispose();

	[Fact]
	public async Task GetLatestAsync_NoSnapshots_ReturnsNull()
	{
		var result = await _repo.GetLatestAsync();
		result.Should().BeNull();
	}

	[Fact]
	public async Task GetLatestAsync_MultipleSnapshots_ReturnsNewest()
	{
		var now = DateTime.UtcNow;
		await Seed.SnapshotAsync(_db, stage: 2, recordedAt: now.AddHours(-3));
		await Seed.SnapshotAsync(_db, stage: 4, recordedAt: now.AddHours(-1)); // newest
		await Seed.SnapshotAsync(_db, stage: 1, recordedAt: now.AddHours(-5));

		var result = await _repo.GetLatestAsync();

		result.Should().NotBeNull();
		result!.Stage.Should().Be(4);
	}

	[Fact]
	public async Task GetCurrentStageAsync_NoSnapshots_ReturnsZero()
	{
		var stage = await _repo.GetCurrentStageAsync();
		stage.Should().Be(0);
	}

	[Fact]
	public async Task GetCurrentStageAsync_WithSnapshots_ReturnsLatestStage()
	{
		var now = DateTime.UtcNow;
		await Seed.SnapshotAsync(_db, stage: 2, recordedAt: now.AddHours(-2));
		await Seed.SnapshotAsync(_db, stage: 6, recordedAt: now.AddMinutes(-5));

		var stage = await _repo.GetCurrentStageAsync();

		stage.Should().Be(6);
	}

	[Fact]
	public async Task AddAsync_ThenSave_PersistsSnapshot()
	{
		var snapshot = new StageSnapshot { Stage = 3, RawText = "4", CreatedAt = DateTime.UtcNow };

		await _repo.UpdateAsync(snapshot);
		await _repo.UnitOfWork.SaveEntitiesAsync();

		var latest = await _repo.GetLatestAsync();
		latest.Should().NotBeNull();
		latest!.Stage.Should().Be(3);
	}

	[Fact]
	public async Task PurgeOlderThanAsync_RemovesOldEntries_KeepsRecent()
	{
		var now = DateTime.UtcNow;
		await Seed.SnapshotAsync(_db, stage: 1, recordedAt: now.AddDays(-10));
		await Seed.SnapshotAsync(_db, stage: 2, recordedAt: now.AddDays(-8));
		await Seed.SnapshotAsync(_db, stage: 3, recordedAt: now.AddDays(-3));   // kept
		await Seed.SnapshotAsync(_db, stage: 4, recordedAt: now.AddHours(-1));  // kept

		await _repo.PurgeOlderThanAsync(TimeSpan.FromDays(7));
		await _repo.UnitOfWork.SaveEntitiesAsync();

		var remaining = await _repo.GetAsync();
		remaining.Should().HaveCount(2);
		remaining.Should().AllSatisfy(s => s.Stage.Should().BeGreaterThanOrEqualTo(3));
	}

	[Fact]
	public async Task PurgeOlderThanAsync_NothingOld_LeavesAllIntact()
	{
		await Seed.SnapshotAsync(_db, stage: 2, recordedAt: DateTime.UtcNow.AddHours(-1));
		await Seed.SnapshotAsync(_db, stage: 3, recordedAt: DateTime.UtcNow.AddMinutes(-5));

		await _repo.PurgeOlderThanAsync(TimeSpan.FromDays(7));
		await _repo.UnitOfWork.SaveEntitiesAsync();

		(await _repo.GetAsync()).Should().HaveCount(2);
	}
}