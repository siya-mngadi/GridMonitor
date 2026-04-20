using FluentAssertions;
using GridMonitor.Domain.Entities;
using GridMonitor.Domain.Repositories;
using GridMonitor.Infrastructure.DataContext;
using GridMonitor.Infrastructure.Repositories;
using GridMonitor.Tests.Unit.Shared;

namespace GridMonitor.Tests.Unit.Repositories;

public class ApiKeyRepositoryTests : IDisposable
{
	private readonly AppDbContext _db = InMemoryDb.Create();
	private readonly IApiKeyRepository _repo;
	public ApiKeyRepositoryTests() => _repo = new ApiKeyRepository(_db);
	public void Dispose() => _db.Dispose();

	[Fact]
	public async Task GetByHashAsync_ActiveKey_ReturnsKey()
	{
		var u = await Seed.UserAsync(_db);
		var k = await Seed.ApiKeyAsync(_db, u.Id, isActive: true);

		var result = await _repo.GetByHashAsync(k.KeyHash);

		result.Should().NotBeNull();
		result!.Id.Should().Be(k.Id);
	}

	[Fact]
	public async Task GetByHashAsync_InactiveKey_ReturnsNull()
	{
		var u = await Seed.UserAsync(_db);
		var k = await Seed.ApiKeyAsync(_db, u.Id, isActive: false);

		var result = await _repo.GetByHashAsync(k.KeyHash);

		result.Should().BeNull("GetByHashAsync filters out inactive keys");
	}

	[Fact]
	public async Task GetByHashAsync_UnknownHash_ReturnsNull()
	{
		var result = await _repo.GetByHashAsync("unknown-hash");
		result.Should().BeNull();
	}

	[Fact]
	public async Task GetByIdAsync_ExistingKey_ReturnsKey()
	{
		var u = await Seed.UserAsync(_db);
		var k = await Seed.ApiKeyAsync(_db, u.Id);

		var result = await _repo.GetByIdAsync(k.Id);

		result.Should().NotBeNull();
		result!.UserId.Should().Be(u.Id);
	}

	[Fact]
	public async Task GetByIdAsync_MissingKey_ReturnsNull()
	{
		var result = await _repo.GetByIdAsync(Guid.NewGuid());
		result.Should().BeNull();
	}

	[Fact]
	public async Task GetByUserAsync_ReturnsOnlyKeysForThatUser()
	{
		var u1 = await Seed.UserAsync(_db);
		var u2 = await Seed.UserAsync(_db);
		await Seed.ApiKeyAsync(_db, u1.Id);
		await Seed.ApiKeyAsync(_db, u1.Id);
		await Seed.ApiKeyAsync(_db, u2.Id);

		var result = await _repo.GetApiKeysAsync(u1.Id);

		result.Should().HaveCount(2);
		result.Should().AllSatisfy(k => k.UserId.Should().Be(u1.Id));
	}

	[Fact]
	public async Task GetByUserAsync_NoKeys_ReturnsEmpty()
	{
		var u = await Seed.UserAsync(_db);
		var result = await _repo.GetApiKeysAsync(u.Id);
		result.Should().BeEmpty();
	}

	[Fact]
	public async Task DeactivateAsync_ActiveKey_SetsIsActiveFalse()
	{
		var u = await Seed.UserAsync(_db);
		var k = await Seed.ApiKeyAsync(_db, u.Id, isActive: true);

		await _repo.DeactivateAsync(k.Id);
		await _repo.UnitOfWork.SaveEntitiesAsync();

		var updated = await _repo.GetByIdAsync(k.Id);
		updated!.Active.Should().BeFalse();
	}

	[Fact]
	public async Task DeactivateAsync_MissingKey_DoesNotThrow()
	{
		var act = async () =>
		{
			await _repo.DeactivateAsync(Guid.NewGuid());
			await _repo.UnitOfWork.SaveEntitiesAsync();
		};
		await act.Should().NotThrowAsync();
	}

	[Fact]
	public async Task AddAsync_ThenSave_PersistsKey()
	{
		var u = await Seed.UserAsync(_db);
		var k = new ApiKey
		{
			Id = Guid.NewGuid(),
			UserId = u.Id,
			KeyHash = "unique-hash-xyz",
			KeyPrefix = "ls_test...",
			Active = true,
			DailyCallLimit = 100,
			CreatedAt = DateTime.UtcNow
		};

		await _repo.AddAsync(k);
		await _repo.UnitOfWork.SaveEntitiesAsync();

		(await _repo.GetByHashAsync("unique-hash-xyz")).Should().NotBeNull();
	}
}
