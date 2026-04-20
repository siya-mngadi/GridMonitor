using FluentAssertions;
using GridMonitor.Domain.Repositories;
using GridMonitor.Infrastructure.DataContext;
using GridMonitor.Infrastructure.Repositories;
using GridMonitor.Tests.Unit.Shared;

namespace GridMonitor.Tests.Unit.Repositories;

public class ProvinceRepositoryTests : IDisposable
{
	private readonly AppDbContext _db = InMemoryDb.Create();
	private readonly IProvinceRepository _repo;
	public ProvinceRepositoryTests() => _repo = new ProvinceRepository(_db);
	public void Dispose() => _db.Dispose();

	[Fact]
	public async Task GetByIdAsync_ExistingId_ReturnsProvince()
	{
		var p = await Seed.ProvinceAsync(_db, eskomId: 1);

		var result = await _repo.GetByEskomIdAsync(p.Id);

		result.Should().NotBeNull();
		result!.EskomId.Should().Be(1);
	}

	[Fact]
	public async Task GetByIdAsync_MissingId_ReturnsNull()
	{
		var result = await _repo.GetByIdAsync(9999);
		result.Should().BeNull();
	}

	[Fact]
	public async Task GetByEskomIdAsync_ExistingEskomId_ReturnsProvince()
	{
		await Seed.ProvinceAsync(_db, eskomId: 9);

		var result = await _repo.GetByEskomIdAsync(9);

		result.Should().NotBeNull();
		result!.Name.Should().Be("Province-9");
	}

	[Fact]
	public async Task GetByEskomIdAsync_MissingEskomId_ReturnsNull()
	{
		var result = await _repo.GetByEskomIdAsync(99);
		result.Should().BeNull();
	}

	[Fact]
	public async Task GetAllAsync_ReturnsAllProvinces()
	{
		await Seed.ProvinceAsync(_db, 1);
		await Seed.ProvinceAsync(_db, 2);
		await Seed.ProvinceAsync(_db, 3);

		var result = await _repo.GetAsync();

		result.Should().HaveCount(3);
	}
}
