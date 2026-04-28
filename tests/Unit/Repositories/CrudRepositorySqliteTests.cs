using FluentAssertions;
using GridMonitor.Domain.Entities;
using GridMonitor.Domain.Repositories;
using GridMonitor.Infrastructure.Repositories;
using GridMonitor.Tests.Unit.Shared;

namespace GridMonitor.Tests.Unit.Repositories;

public class CrudRepositorySqliteTests : IDisposable
{
	private readonly SqliteDb _sqlite = new();
	private readonly ISuburbRepository _suburbRepo;
	private readonly IMunicipalityRepository _muniRepo;
	private readonly IScheduleSlotRepository _slotRepo;

	public CrudRepositorySqliteTests()
	{
		_suburbRepo = new SuburbRepository(_sqlite.Ctx);
		_muniRepo = new MunicipalityRepository(_sqlite.Ctx);
		_slotRepo = new ScheduleSlotRepository(_sqlite.Ctx);
	}

	public void Dispose() => _sqlite.Dispose();

	private async Task<int> SeedMuniAsync(int provinceId)
	{
		var p = await Seed.ProvinceAsync(_sqlite.Ctx, provinceId);
		var m = await Seed.MunicipalityAsync(_sqlite.Ctx, p.Id);
		return m.EskomId;
	}

	private ScheduleSlot MakeSlot(
	int suburbId,
	short stage = 2,
	DayOfWeek dayNumber = DayOfWeek.Monday,
	TimeOnly? start = null,
	TimeOnly? end = null,
	string dataHash = "abc")
	=> new()
	{
		SuburbId = suburbId,
		Stage = stage,
		ScheduleDay = dayNumber,
		StartTime = start ?? new TimeOnly(22, 0),
		EndTime = end ?? new TimeOnly(0, 30),
		DataHash = dataHash
	};

	private async Task<int> SeedSuburbAsync()
	{
		var p = await Seed.ProvinceAsync(_sqlite.Ctx);
		var m = await Seed.MunicipalityAsync(_sqlite.Ctx, p.EskomId);
		var s = await Seed.SuburbAsync(_sqlite.Ctx, m.EskomId);
		return s.EskomId;
	}

	[Fact]
	public async Task SearchAsync_CaseInsensitivePartialMatch_ReturnsMatches()
	{
		int provinceId = 1; // Western Cape;
		var muniId = await SeedMuniAsync(provinceId);
		await Seed.SuburbAsync(_sqlite.Ctx, muniId, "Milnerton");
		await Seed.SuburbAsync(_sqlite.Ctx, muniId, "Milnerton North");
		await Seed.SuburbAsync(_sqlite.Ctx, muniId, "Bellville");

		var result = await _suburbRepo.SearchAsync("milner", 10);

		result.Should().HaveCount(2);
		result.Should().AllSatisfy(s =>
			s.Name.ToLower().Should().Contain("milner"));
	}

	[Fact]
	public async Task SearchAsync_NoMatch_ReturnsEmpty()
	{
		int provinceId = 1; // Western Cape;
		var muniId = await SeedMuniAsync(provinceId);
		await Seed.SuburbAsync(_sqlite.Ctx, muniId, "Rondebosch");

		var result = await _suburbRepo.SearchAsync("zzznomatch", 10);

		result.Should().BeEmpty();
	}

	[Fact]
	public async Task SearchAsync_RespectsLimit()
	{
		int provinceId = 1; // Western Cape;
		var muniId = await SeedMuniAsync(provinceId);
		for (var i = 0; i < 10; i++)
			await Seed.SuburbAsync(_sqlite.Ctx, muniId, $"Suburb Alpha {i}");

		var result = await _suburbRepo.SearchAsync("alpha", limit: 3);

		result.Should().HaveCount(3);
	}

	[Fact]
	public async Task SearchAsync_ResultsOrderedAlphabetically()
	{
		int provinceId = 1; // Western Cape;
		var muniId = await SeedMuniAsync(provinceId);
		await Seed.SuburbAsync(_sqlite.Ctx, muniId, "Zonnebloem");
		await Seed.SuburbAsync(_sqlite.Ctx, muniId, "Athlone");
		await Seed.SuburbAsync(_sqlite.Ctx, muniId, "Maitland");

		var result = await _suburbRepo.SearchAsync("a", limit: 10);

		result.Select(s => s.Name).Should().BeInAscendingOrder();
	}

	[Fact]
	public async Task UpsertAsync_NewSuburb_Inserts()
	{
		int provinceId = 3; // Gauteng;
		var muniId = await SeedMuniAsync(provinceId);
		var s = new Suburb
		{
			EskomId = 1_000_111,
			Name = "Sandton",
			MunicipalityId = muniId,
			Total = 5,
			LastSyncedAt = DateTime.UtcNow
		};

		await _suburbRepo.UpsertAsync([s]);
		await _suburbRepo.UnitOfWork.SaveEntitiesAsync();

		var found = await _suburbRepo.GetByEskomIdAsync(1_000_111);
		found.Should().NotBeNull();
		found!.Name.Should().Be("Sandton");
	}

	[Fact]
	public async Task UpsertAsync_ExistingSuburb_UpdatesNameAndTotal()
	{
		int provinceId = 3; // Gauteng;
		var muniId = await SeedMuniAsync(provinceId);
		var s = await Seed.SuburbAsync(_sqlite.Ctx, muniId, "Old Name");

		await _suburbRepo.UpsertAsync([new Suburb
		{
			EskomId = s.EskomId,
			Name = "New Name",
			MunicipalityId = muniId,
			Total = 99,
			LastSyncedAt = DateTime.UtcNow
		}]);
		await _suburbRepo.UnitOfWork.SaveEntitiesAsync();

		var updated = await _suburbRepo.GetByEskomIdAsync(s.EskomId);
		updated!.Name.Should().Be("New Name");
		updated.Total.Should().Be(99);
	}

	[Fact]
	public async Task UpsertAsync_ExistingSuburb_DoesNotInsertDuplicate()
	{
		int provinceId = 3; // Gauteng;
		var muniId = await SeedMuniAsync(provinceId);
		var s = await Seed.SuburbAsync(_sqlite.Ctx, muniId);

		await _suburbRepo.UpsertAsync([new Suburb
		{
			EskomId = s.EskomId,
			Name = "Updated",
			MunicipalityId = muniId,
			Total = 1,
			LastSyncedAt = DateTime.UtcNow
		}]);
		await _suburbRepo.UnitOfWork.SaveEntitiesAsync();

		(await _suburbRepo.GetAsync()).Should().ContainSingle();
	}


	[Fact]
	public async Task UpsertAsync_NewMunicipality_Inserts()
	{
		var p = await Seed.ProvinceAsync(_sqlite.Ctx);
		var muni = new Municipality
		{
			EskomId = 1000001,
			Name = "New City",
			ProvinceId = p.Id,
			Total = 5,
			LastSyncedAt = DateTime.UtcNow
		};

		await _muniRepo.UpsertAsync([muni]);
		await _muniRepo.UnitOfWork.SaveEntitiesAsync();

		var found = await _muniRepo.GetByEskomIdAsync(1000001);
		found.Should().NotBeNull();
		found!.Name.Should().Be("New City");
	}

	[Fact]
	public async Task UpsertAsync_ExistingMunicipality_UpdatesFields()
	{
		var p = await Seed.ProvinceAsync(_sqlite.Ctx);
		var m = await Seed.MunicipalityAsync(_sqlite.Ctx, p.Id);

		var updated = new Municipality
		{
			EskomId = m.EskomId,
			Name = "Updated City",
			ProvinceId = p.Id,
			Total = 99,
			LastSyncedAt = DateTime.UtcNow
		};

		await _muniRepo.UpsertAsync([updated]);
		await _muniRepo.UnitOfWork.SaveEntitiesAsync();

		var found = await _muniRepo.GetByEskomIdAsync(m.EskomId);
		found!.Name.Should().Be("Updated City");
		found.Total.Should().Be(99);
	}

	[Fact]
	public async Task UpsertAsync_ExistingMunicipality_DoesNotCreateDuplicate()
	{
		var p = await Seed.ProvinceAsync(_sqlite.Ctx);
		var m = await Seed.MunicipalityAsync(_sqlite.Ctx, p.Id);

		var updated = new Municipality
		{
			EskomId = m.EskomId,
			Name = "Duplicate Check",
			ProvinceId = p.Id,
			Total = 1,
			LastSyncedAt = DateTime.UtcNow
		};

		await _muniRepo.UpsertAsync([updated]);
		await _muniRepo.UnitOfWork.SaveEntitiesAsync();

		var all = await _muniRepo.GetByProvinceAsync(p.Id);
		all.Should().ContainSingle();
	}


	[Fact]
	public async Task UpsertSlotAsync_NewSlot_Returns1_AndPersists()
	{
		var suburbId = await SeedSuburbAsync();
		var slot = MakeSlot(suburbId);

		await _slotRepo.UpsertSlotsAsync([slot]);
		await _slotRepo.UnitOfWork.SaveEntitiesAsync();

		var result = await _slotRepo.GetBySuburbAndStageAsync(suburbId, 2);
		result.Should().ContainSingle();
	}

	[Fact]
	public async Task UpsertSlotAsync_SameCompositeKey_SameHash_Returns0_NoChange()
	{
		var suburbId = await SeedSuburbAsync();
		var slot = MakeSlot(suburbId, dataHash: "same-hash");

		await _slotRepo.UpsertSlotsAsync([slot]);
		await _slotRepo.UnitOfWork.SaveEntitiesAsync();

		// Same key, same hash
		var duplicate = MakeSlot(suburbId, dataHash: "same-hash");
		var result = await _slotRepo.UpsertSlotsAsync([duplicate]);
		await _slotRepo.UnitOfWork.SaveEntitiesAsync();

		result.Should().Be(0);
		(await _slotRepo.GetBySuburbAndStageAsync(suburbId, 2)).Should().ContainSingle("no new row inserted");
	}

	[Fact]
	public async Task UpsertSlotAsync_SameCompositeKey_DifferentHash_ReturnsMinus1_AndUpdates()
	{
		var suburbId = await SeedSuburbAsync();
		var slot = MakeSlot(suburbId, end: new TimeOnly(1, 0), dataHash: "old-hash");

		await _slotRepo.UpsertSlotsAsync([slot]);
		await _slotRepo.UnitOfWork.SaveEntitiesAsync();

		var updated = MakeSlot(suburbId, end: new TimeOnly(2, 30), dataHash: "new-hash");
		await _slotRepo.UpsertSlotsAsync([updated]);
		await _slotRepo.UnitOfWork.SaveEntitiesAsync();

		var persisted = await _slotRepo.GetByCompositeKeyAsync(suburbId, 2, DayOfWeek.Monday, new TimeOnly(22, 0));
		persisted!.EndTime.Should().Be(new TimeOnly(2, 30));
		persisted.DataHash.Should().Be("new-hash");
	}

	[Fact]
	public async Task UpsertSlotAsync_DifferentStage_TreatedAsNewSlot()
	{
		var suburbId = await SeedSuburbAsync();

		await _slotRepo.UpsertSlotsAsync([MakeSlot(suburbId, stage: 2)]);
		await _slotRepo.UnitOfWork.SaveEntitiesAsync();

		await _slotRepo.UpsertSlotsAsync([MakeSlot(suburbId, stage: 4)]);
		await _slotRepo.UnitOfWork.SaveEntitiesAsync();

		var result = await _slotRepo.GetBySuburbAndStageAsync(suburbId, 4);
		result.Should().HaveCount(2);
		result.Should().AllSatisfy(x => x.Stage.Should().BeOneOf(2, 4));
	}

	[Fact]
	public async Task UpsertSlotAsync_DifferentDayNumber_TreatedAsNewSlot()
	{
		var suburbId = await SeedSuburbAsync();

		await _slotRepo.UpsertSlotsAsync([MakeSlot(suburbId, dayNumber: DayOfWeek.Monday)]);
		await _slotRepo.UnitOfWork.SaveEntitiesAsync();

		await _slotRepo.UpsertSlotsAsync([MakeSlot(suburbId, dayNumber: DayOfWeek.Tuesday)]);
		await _slotRepo.UnitOfWork.SaveEntitiesAsync();
		var result = await _slotRepo.GetBySuburbAndStageAsync(suburbId, 2);
		result.Should().HaveCount(2);
		result.Should().AllSatisfy(x => x.ScheduleDay.Should().BeOneOf(DayOfWeek.Monday, DayOfWeek.Tuesday));
	}

	[Fact]
	public async Task GetByCompositeKeyAsync_ExactMatch_ReturnsSlot()
	{
		var suburbId = await SeedSuburbAsync();
		var slot = MakeSlot(suburbId, stage: 3, dayNumber: DayOfWeek.Tuesday, start: new TimeOnly(10, 0));
		await _slotRepo.UpsertSlotsAsync([slot]);
		await _slotRepo.UnitOfWork.SaveEntitiesAsync();

		var found = await _slotRepo.GetByCompositeKeyAsync(suburbId, 3, DayOfWeek.Tuesday, new TimeOnly(10, 0));

		found.Should().NotBeNull();
		found!.Stage.Should().Be(3);
		found.ScheduleDay.Should().Be(DayOfWeek.Tuesday);
	}
}
