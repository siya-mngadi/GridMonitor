using FluentAssertions;
using GridMonitor.Domain.Entities;
using GridMonitor.Domain.Enums;
using GridMonitor.Domain.Repositories;
using GridMonitor.Infrastructure.DataContext;
using GridMonitor.Infrastructure.Repositories;
using GridMonitor.Tests.Unit.Shared;

namespace GridMonitor.Tests.Unit.Repositories;

public class SubscriptionRepositoryTests : IDisposable
{
	private readonly AppDbContext _db = InMemoryDb.Create();
	private readonly IAlertSubscriptionRepository _repo;
	public SubscriptionRepositoryTests() => _repo = new AlertSubscriptionRepository(_db);
	public void Dispose() => _db.Dispose();

	private async Task<(User user, int suburbId)> SeedUserAndSuburbAsync()
	{
		var p = await Seed.ProvinceAsync(_db);
		var m = await Seed.MunicipalityAsync(_db, p.Id);
		var s = await Seed.SuburbAsync(_db, m.Id);
		var u = await Seed.UserAsync(_db);
		return (u, s.Id);
	}

	[Fact]
	public async Task GetByIdAsync_ExistingSubscription_ReturnsIt()
	{
		var (u, subId) = await SeedUserAndSuburbAsync();
		var sub = await Seed.SubscriptionAsync(_db, u.Id, subId);

		var result = await _repo.GetByIdAsync(sub.Id);

		result.Should().NotBeNull();
		result!.SuburbId.Should().Be(subId);
	}

	[Fact]
	public async Task GetByIdAsync_MissingSubscription_ReturnsNull()
	{
		var result = await _repo.GetByIdAsync(Guid.NewGuid());
		result.Should().BeNull();
	}

	[Fact]
	public async Task GetByUserAndSuburbAsync_ExistingCombination_ReturnsSubscription()
	{
		var (u, subId) = await SeedUserAndSuburbAsync();
		await Seed.SubscriptionAsync(_db, u.Id, subId);

		var result = await _repo.GetByUserAndSuburbAsync(u.Id, subId);

		result.Should().NotBeNull();
		result!.UserId.Should().Be(u.Id);
		result.SuburbId.Should().Be(subId);
	}

	[Fact]
	public async Task GetByUserAndSuburbAsync_WrongUser_ReturnsNull()
	{
		var (u, subId) = await SeedUserAndSuburbAsync();
		await Seed.SubscriptionAsync(_db, u.Id, subId);

		var result = await _repo.GetByUserAndSuburbAsync(Guid.NewGuid(), subId);

		result.Should().BeNull();
	}

	[Fact]
	public async Task GetByUserAsync_IncludesSuburbAndChannels()
	{
		var (u, subId) = await SeedUserAndSuburbAsync();
		var sub = await Seed.SubscriptionAsync(_db, u.Id, subId);
		await Seed.ChannelAsync(_db, sub.Id, ChannelType.Email, "t@test.com");

		var result = await _repo.GetByUserAsync(u.Id);

		result.Should().ContainSingle();
		result[0].Suburb.Should().NotBeNull();
		result[0].Channels.Should().ContainSingle();
	}

	[Fact]
	public async Task GetByUserAsync_OnlyReturnsCallerSubscriptions()
	{
		var (u1, sub1Id) = await SeedUserAndSuburbAsync();
		var p = await Seed.ProvinceAsync(_db, eskomId: 2);
		var m = await Seed.MunicipalityAsync(_db, p.Id);
		var s2 = await Seed.SuburbAsync(_db, m.Id);
		var u2 = await Seed.UserAsync(_db);

		await Seed.SubscriptionAsync(_db, u1.Id, sub1Id);
		await Seed.SubscriptionAsync(_db, u2.Id, s2.Id);

		var result = await _repo.GetByUserAsync(u1.Id);

		result.Should().ContainSingle();
		result[0].UserId.Should().Be(u1.Id);
	}

	[Fact]
	public async Task GetAllActiveWithDetailsAsync_ExcludesInactive()
	{
		var (u, subId) = await SeedUserAndSuburbAsync();
		await Seed.SubscriptionAsync(_db, u.Id, subId, isActive: true);
		await Seed.SubscriptionAsync(_db, u.Id, subId + 999, isActive: false);

		var result = await _repo.GetAllActiveWithDetailsAsync();

		result.Should().ContainSingle();
		result[0].Active.Should().BeTrue();
	}

	[Fact]
	public async Task GetAllActiveWithDetailsAsync_ExcludesInactiveChannels()
	{
		var (u, subId) = await SeedUserAndSuburbAsync();
		var sub = await Seed.SubscriptionAsync(_db, u.Id, subId);
		await Seed.ChannelAsync(_db, sub.Id, isActive: true);
		await Seed.ChannelAsync(_db, sub.Id, isActive: false);

		var result = await _repo.GetAllActiveWithDetailsAsync();

		result.Should().ContainSingle();
		result[0].Channels.Should().ContainSingle("only active channels are included");
	}

	[Fact]
	public async Task AddAsync_ThenSave_PersistsSubscription()
	{
		var (u, subId) = await SeedUserAndSuburbAsync();
		var sub = new AlertSubscription
		{
			Id = Guid.NewGuid(),
			UserId = u.Id,
			SuburbId = subId,
			AlertMinutesBefore = 30,
			Active = true,
			CreatedAt = DateTime.UtcNow
		};

		await _repo.AddAsync(sub);
		await _repo.UnitOfWork.SaveEntitiesAsync();

		(await _repo.GetByUserAndSuburbAsync(u.Id, subId)).Should().NotBeNull();
	}
}