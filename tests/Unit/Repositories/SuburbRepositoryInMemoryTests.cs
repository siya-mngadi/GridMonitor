using FluentAssertions;
using GridMonitor.Domain.Entities;
using GridMonitor.Domain.Repositories;
using GridMonitor.Infrastructure.DataContext;
using GridMonitor.Infrastructure.Repositories;
using GridMonitor.Tests.Unit.Shared;

namespace GridMonitor.Tests.Unit.Repositories;

public class SuburbRepositoryInMemoryTests : IDisposable
{
	private readonly AppDbContext _db = InMemoryDb.Create();
	private readonly ISuburbRepository _repo;
	public SuburbRepositoryInMemoryTests() => _repo = new SuburbRepository(_db);
	public void Dispose() => _db.Dispose();

	private async Task<int> SeedMuniAsync()
	{
		var p = await Seed.ProvinceAsync(_db);
		var m = await Seed.MunicipalityAsync(_db, p.Id);
		return m.Id;
	}

	[Fact]
	public async Task GetByIdAsync_ExistingId_ReturnsSuburb()
	{
		var muniId = await SeedMuniAsync();
		var s = await Seed.SuburbAsync(_db, muniId, "Observatory");

		var result = await _repo.GetByIdAsync(s.Id);

		result.Should().NotBeNull();
		result!.Name.Should().Be("Observatory");
	}

	[Fact]
	public async Task GetByIdAsync_MissingId_ReturnsNull()
	{
		var result = await _repo.GetByIdAsync(99999);
		result.Should().BeNull();
	}

	[Fact]
	public async Task GetByEskomIdAsync_ExistingId_ReturnsSuburb()
	{
		var muniId = await SeedMuniAsync();
		var s = await Seed.SuburbAsync(_db, muniId);

		var result = await _repo.GetByEskomIdAsync(s.EskomId);

		result.Should().NotBeNull();
		result!.Id.Should().Be(s.Id);
	}

	[Fact]
	public async Task GetByEskomIdAsync_MissingId_ReturnsNull()
	{
		var result = await _repo.GetByEskomIdAsync(100_002);
		result.Should().BeNull();
	}

	[Fact]
	public async Task GetByMunicipalityAsync_ReturnsSuburbsAlphabetically()
	{
		var muniId = await SeedMuniAsync();
		await Seed.SuburbAsync(_db, muniId, "Woodstock");
		await Seed.SuburbAsync(_db, muniId, "Athlone");
		await Seed.SuburbAsync(_db, muniId, "Maitland");

		var result = await _repo.GetByMunicipalityAsync(muniId);

		result.Should().HaveCount(3);
		result.Select(s => s.Name).Should().BeInAscendingOrder();
	}

	[Fact]
	public async Task GetByMunicipalityAsync_DifferentMunicipality_ReturnsEmpty()
	{
		var muniId = await SeedMuniAsync();
		await Seed.SuburbAsync(_db, muniId, "Rondebosch");

		var result = await _repo.GetByMunicipalityAsync(muniId + 999);

		result.Should().BeEmpty();
	}

	[Fact]
	public async Task GetWithSlotsAsync_IncludesSlots()
	{
		var muniId = await SeedMuniAsync();
		var s = await Seed.SuburbAsync(_db, muniId, "Bellville");
		_db.ScheduleSlots.Add(new ScheduleSlot
		{
			SuburbId = s.Id,
			Stage = 2,
			StartTime = new TimeOnly(22, 0),
			EndTime = new TimeOnly(0, 30),
			ScheduleDay = DayOfWeek.Monday,
			DataHash = "h1"
		});
		await _db.SaveChangesAsync();

		var result = await _repo.GetByIdAsync(s.Id);

		result.Should().NotBeNull();
		result!.Slots.Should().HaveCount(1);
	}

	[Fact]
	public async Task UpsertAsync_NewSuburb_Inserts()
	{
		var muniId = await SeedMuniAsync();
		var s = new Suburb
		{
			EskomId = 1_000_111,
			Name = "Sandton",
			MunicipalityId = muniId,
			Total = 5,
			LastSyncedAt = DateTime.UtcNow
		};

		await _repo.UpsertAsync([s]);
		await _repo.UnitOfWork.SaveEntitiesAsync();

		var found = await _repo.GetByEskomIdAsync(1_000_111);
		found.Should().NotBeNull();
		found!.Name.Should().Be("Sandton");
	}

	[Fact]
	public async Task UpsertAsync_ExistingSuburb_UpdatesNameAndTotal()
	{
		var muniId = await SeedMuniAsync();
		var s = await Seed.SuburbAsync(_db, muniId, "Old Name");

		await _repo.UpsertAsync([new Suburb
		{
			EskomId = s.EskomId,
			Name = "New Name",
			MunicipalityId = muniId,
			Total = 99,
			LastSyncedAt = DateTime.UtcNow
		}]);
		await _repo.UnitOfWork.SaveEntitiesAsync();

		var updated = await _repo.GetByEskomIdAsync(s.EskomId);
		updated!.Name.Should().Be("New Name");
		updated.Total.Should().Be(99);
	}

	[Fact]
	public async Task UpsertAsync_ExistingSuburb_DoesNotInsertDuplicate()
	{
		var muniId = await SeedMuniAsync();
		var s = await Seed.SuburbAsync(_db, muniId);

		await _repo.UpsertAsync([new Suburb
		{
			EskomId = s.EskomId,
			Name = "Updated",
			MunicipalityId = muniId,
			Total = 1,
			LastSyncedAt = DateTime.UtcNow
		}]);
		await _repo.UnitOfWork.SaveEntitiesAsync();

		(await _repo.GetAsync()).Should().ContainSingle();
	}
}
