using FluentAssertions;
using GridMonitor.Domain.Entities;
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

	[Fact]
	public async Task GetAllWithSuburbsAsync_IncludesSuburbs()
	{
		var p = await Seed.ProvinceAsync(_db);
		var m = await Seed.MunicipalityAsync(_db, p.Id);
		await Seed.SuburbAsync(_db, m.Id);
		await Seed.SuburbAsync(_db, m.Id);

		var result = await _repo.GetByProvinceAsync(p.Id);

		result.Should().ContainSingle();
		result[0].Suburbs.Should().HaveCount(2);
	}

	[Fact]
	public async Task UpsertAsync_NewMunicipality_Inserts()
	{
		var p = await Seed.ProvinceAsync(_db);
		var muni = new Municipality
		{
			EskomId = 1000001,
			Name = "New City",
			ProvinceId = p.Id,
			Total = 5,
			LastSyncedAt = DateTime.UtcNow
		};

		await _repo.UpsertAsync([muni]);
		await _repo.UnitOfWork.SaveEntitiesAsync();

		var found = await _repo.GetByEskomIdAsync(1000001);
		found.Should().NotBeNull();
		found!.Name.Should().Be("New City");
	}

	[Fact]
	public async Task UpsertAsync_ExistingMunicipality_UpdatesFields()
	{
		var p = await Seed.ProvinceAsync(_db);
		var m = await Seed.MunicipalityAsync(_db, p.Id);

		var updated = new Municipality
		{
			EskomId = m.EskomId,
			Name = "Updated City",
			ProvinceId = p.Id,
			Total = 99,
			LastSyncedAt = DateTime.UtcNow
		};

		await _repo.UpsertAsync([updated]);
		await _repo.UnitOfWork.SaveEntitiesAsync();

		var found = await _repo.GetByEskomIdAsync(m.EskomId);
		found!.Name.Should().Be("Updated City");
		found.Total.Should().Be(99);
	}

	[Fact]
	public async Task UpsertAsync_ExistingMunicipality_DoesNotCreateDuplicate()
	{
		var p = await Seed.ProvinceAsync(_db);
		var m = await Seed.MunicipalityAsync(_db, p.Id);

		var updated = new Municipality
		{
			EskomId = m.EskomId,
			Name = "Duplicate Check",
			ProvinceId = p.Id,
			Total = 1,
			LastSyncedAt = DateTime.UtcNow
		};

		await _repo.UpsertAsync([updated]);
		await _repo.UnitOfWork.SaveEntitiesAsync();

		var all = await _repo.GetByProvinceAsync(p.Id);
		all.Should().ContainSingle();
	}
}
