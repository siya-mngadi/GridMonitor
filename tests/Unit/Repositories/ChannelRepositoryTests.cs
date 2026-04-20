using FluentAssertions;
using GridMonitor.Domain.Entities;
using GridMonitor.Domain.Enums;
using GridMonitor.Domain.Repositories;
using GridMonitor.Infrastructure.DataContext;
using GridMonitor.Infrastructure.Repositories;
using GridMonitor.Tests.Unit.Shared;

namespace GridMonitor.Tests.Unit.Repositories;

public class ChannelRepositoryTests : IDisposable
{
	private readonly AppDbContext _db = InMemoryDb.Create();
	private readonly IAlertChannelRepository _repo;
	public ChannelRepositoryTests() => _repo = new AlertChannelRepository(_db);
	public void Dispose() => _db.Dispose();

	private async Task<AlertSubscription> SeedSubscriptionAsync()
	{
		var p = await Seed.ProvinceAsync(_db);
		var m = await Seed.MunicipalityAsync(_db, p.Id);
		var s = await Seed.SuburbAsync(_db, m.Id);
		var u = await Seed.UserAsync(_db);
		return await Seed.SubscriptionAsync(_db, u.Id, s.Id);
	}

	[Fact]
	public async Task GetByIdAsync_ExistingChannel_ReturnsIt()
	{
		var sub = await SeedSubscriptionAsync();
		var ch = await Seed.ChannelAsync(_db, sub.Id, ChannelType.Email, "t@test.com");

		var result = await _repo.GetByIdAsync(ch.Id);

		result.Should().NotBeNull();
		result!.ChannelType.Should().Be(ChannelType.Email);
	}

	[Fact]
	public async Task GetByIdAsync_MissingId_ReturnsNull()
	{
		var result = await _repo.GetByIdAsync(Guid.NewGuid());
		result.Should().BeNull();
	}

	[Fact]
	public async Task GetBySubscriptionAsync_ReturnsOnlyActiveChannels()
	{
		var sub = await SeedSubscriptionAsync();
		await Seed.ChannelAsync(_db, sub.Id, isActive: true);
		await Seed.ChannelAsync(_db, sub.Id, isActive: false);

		var result = await _repo.GetBySubscriptionAsync(sub.Id);

		result.Should().ContainSingle();
		result[0].Active.Should().BeTrue();
	}

	[Fact]
	public async Task GetBySubscriptionAsync_DifferentSubscription_ReturnsEmpty()
	{
		var sub = await SeedSubscriptionAsync();
		await Seed.ChannelAsync(_db, sub.Id);

		var result = await _repo.GetBySubscriptionAsync(Guid.NewGuid());

		result.Should().BeEmpty();
	}

	[Fact]
	public async Task DeactivateAsync_ActiveChannel_SetsIsActiveFalse()
	{
		var sub = await SeedSubscriptionAsync();
		var ch = await Seed.ChannelAsync(_db, sub.Id, isActive: true);

		await _repo.DeactivateAsync(ch.Id);
		await _repo.UnitOfWork.SaveEntitiesAsync();

		var updated = await _repo.GetByIdAsync(ch.Id);
		updated!.Active.Should().BeFalse();
	}

	[Fact]
	public async Task DeactivateAsync_MissingId_DoesNotThrow()
	{
		var act = async () =>
		{
			await _repo.DeactivateAsync(Guid.NewGuid());
			await _repo.UnitOfWork.SaveEntitiesAsync();
		};
		await act.Should().NotThrowAsync();
	}

	[Fact]
	public async Task AddAsync_ThenSave_PersistsChannel()
	{
		var sub = await SeedSubscriptionAsync();
		var ch = new AlertChannel
		{
			Id = Guid.NewGuid(),
			SubscriptionId = sub.Id,
			ChannelType = ChannelType.Webhook,
			Destination = "https://example.com",
			WebhookSecret = "secret",
			Active = true
		};

		await _repo.AddAsync(ch);
		await _repo.UnitOfWork.SaveEntitiesAsync();

		var channels = await _repo.GetBySubscriptionAsync(sub.Id);
		channels.Should().ContainSingle();
		channels[0].ChannelType.Should().Be(ChannelType.Webhook);
	}
}