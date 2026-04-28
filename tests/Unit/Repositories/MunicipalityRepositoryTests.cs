using FluentAssertions;
using GridMonitor.Domain.Repositories;
using GridMonitor.Infrastructure.DataContext;
using GridMonitor.Infrastructure.Repositories;
using GridMonitor.Tests.Unit.Shared;

namespace GridMonitor.Tests.Unit.Repositories;

public class MunicipalityRepositoryTests : IDisposable
{
	private readonly AppDbContext _db = InMemoryDb.Create();
	private readonly IMunicipalityRepository _repo;
	public MunicipalityRepositoryTests() => _repo = new MunicipalityRepository(_db);
	public void Dispose() => _db.Dispose();

	[Fact]
	public async Task GetByEskomIdAsync_ExistingId_ReturnsMunicipality()
	{
		var p = await Seed.ProvinceAsync(_db);
		var m = await Seed.MunicipalityAsync(_db, p.Id);

		var result = await _repo.GetByEskomIdAsync(m.EskomId);

		result.Should().NotBeNull();
		result!.Name.Should().Be(m.Name);
	}

	[Fact]
	public async Task GetByEskomIdAsync_MissingId_ReturnsNull()
	{
		var result = await _repo.GetByEskomIdAsync(99999999);
		result.Should().BeNull();
	}

	[Fact]
	public async Task GetByProvinceAsync_ReturnsOnlyMatchingProvince()
	{
		var p1 = await Seed.ProvinceAsync(_db, 1);
		var p2 = await Seed.ProvinceAsync(_db, 2);
		await Seed.MunicipalityAsync(_db, p1.Id);
		await Seed.MunicipalityAsync(_db, p1.Id);
		await Seed.MunicipalityAsync(_db, p2.Id);

		var result = await _repo.GetByProvinceAsync(p1.Id);

		result.Should().HaveCount(2);
		result.Should().AllSatisfy(m => m.ProvinceId.Should().Be(p1.Id));
	}
}
