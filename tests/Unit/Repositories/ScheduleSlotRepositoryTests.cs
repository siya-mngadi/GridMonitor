using FluentAssertions;
using GridMonitor.Domain.Entities;
using GridMonitor.Domain.Repositories;
using GridMonitor.Infrastructure.DataContext;
using GridMonitor.Infrastructure.Repositories;
using GridMonitor.Tests.Unit.Shared;

namespace GridMonitor.Tests.Unit.Repositories;

public class ScheduleSlotRepositoryTests : IDisposable
{
	private readonly AppDbContext _db = InMemoryDb.Create();
	private readonly IScheduleSlotRepository _repo;
	public ScheduleSlotRepositoryTests() => _repo = new ScheduleSlotRepository(_db);
	public void Dispose() => _db.Dispose();

	private async Task<int> SeedSuburbAsync()
	{
		var p = await Seed.ProvinceAsync(_db);
		var m = await Seed.MunicipalityAsync(_db, p.Id);
		var s = await Seed.SuburbAsync(_db, m.Id);
		return s.Id;
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

	[Fact]
	public async Task UpsertSlotAsync_NewSlot_Returns1_AndPersists()
	{
		var suburbId = await SeedSuburbAsync();
		var slot = MakeSlot(suburbId);

		var result = await _repo.UpsertSlotsAsync([slot]);
		await _repo.UnitOfWork.SaveEntitiesAsync();

		result.Should().Be(1);
		(await _repo.GetBySuburbAndStageAsync(suburbId, 2)).Should().ContainSingle();
	}

	[Fact]
	public async Task UpsertSlotAsync_SameCompositeKey_SameHash_Returns0_NoChange()
	{
		var suburbId = await SeedSuburbAsync();
		var slot = MakeSlot(suburbId, dataHash: "same-hash");

		await _repo.UpsertSlotsAsync([slot]);
		await _repo.UnitOfWork.SaveEntitiesAsync();

		// Same key, same hash
		var duplicate = MakeSlot(suburbId, dataHash: "same-hash");
		var result = await _repo.UpsertSlotsAsync([duplicate]);
		await _repo.UnitOfWork.SaveEntitiesAsync();

		result.Should().Be(0);
		(await _repo.GetBySuburbAndStageAsync(suburbId, 2)).Should().ContainSingle("no new row inserted");
	}

	[Fact]
	public async Task UpsertSlotAsync_SameCompositeKey_DifferentHash_ReturnsMinus1_AndUpdates()
	{
		var suburbId = await SeedSuburbAsync();
		var slot = MakeSlot(suburbId, end: new TimeOnly(1, 0), dataHash: "old-hash");

		await _repo.UpsertSlotsAsync([slot]);
		await _repo.UnitOfWork.SaveEntitiesAsync();

		var updated = MakeSlot(suburbId, end: new TimeOnly(2, 30), dataHash: "new-hash");
		var result = await _repo.UpsertSlotsAsync([updated]);
		await _repo.UnitOfWork.SaveEntitiesAsync();

		result.Should().Be(-1);

		var persisted = await _repo.GetByCompositeKeyAsync(suburbId, 2, DayOfWeek.Monday, new TimeOnly(22, 0));
		persisted!.EndTime.Should().Be(new TimeOnly(2, 30));
		persisted.DataHash.Should().Be("new-hash");
	}

	[Fact]
	public async Task UpsertSlotAsync_DifferentStage_TreatedAsNewSlot()
	{
		var suburbId = await SeedSuburbAsync();

		await _repo.UpsertSlotsAsync([MakeSlot(suburbId, stage: 2)]);
		await _repo.UnitOfWork.SaveEntitiesAsync();

		var result = await _repo.UpsertSlotsAsync([MakeSlot(suburbId, stage: 4)]);
		await _repo.UnitOfWork.SaveEntitiesAsync();
		result.Should().Be(1, "different stage = different composite key = new row");
		(await _repo.GetBySuburbAndStageAsync(suburbId, 2)).Should().HaveCount(2);
	}

	[Fact]
	public async Task UpsertSlotAsync_DifferentDayNumber_TreatedAsNewSlot()
	{
		var suburbId = await SeedSuburbAsync();

		await _repo.UpsertSlotsAsync([MakeSlot(suburbId, dayNumber: DayOfWeek.Monday)]);
		await _repo.UnitOfWork.SaveEntitiesAsync();

		var result = await _repo.UpsertSlotsAsync([MakeSlot(suburbId, dayNumber: DayOfWeek.Tuesday)]);
		await _repo.UnitOfWork.SaveEntitiesAsync();
		result.Should().Be(1);
		(await _repo.GetBySuburbAndStageAsync(suburbId, 2)).Should().HaveCount(2);
	}

	[Fact]
	public async Task GetByCompositeKeyAsync_ExactMatch_ReturnsSlot()
	{
		var suburbId = await SeedSuburbAsync();
		var slot = MakeSlot(suburbId, stage: 3, dayNumber: DayOfWeek.Tuesday, start: new TimeOnly(10, 0));
		await _repo.UpsertSlotsAsync([slot]);
		await _repo.UnitOfWork.SaveEntitiesAsync();

		var found = await _repo.GetByCompositeKeyAsync(suburbId, 3, DayOfWeek.Tuesday, new TimeOnly(10, 0));

		found.Should().NotBeNull();
		found!.Stage.Should().Be(3);
		found.ScheduleDay.Should().Be(DayOfWeek.Tuesday);
	}

	[Fact]
	public async Task GetByCompositeKeyAsync_NoMatch_ReturnsNull()
	{
		var suburbId = await SeedSuburbAsync();

		var found = await _repo.GetByCompositeKeyAsync(suburbId, 2, DayOfWeek.Monday, new TimeOnly(22, 0));

		found.Should().BeNull();
	}

	[Fact]
	public async Task GetBySuburbAndStageAsync_ReturnsSlots_AtOrBelowCurrentStage()
	{
		var suburbId = await SeedSuburbAsync();
		_db.ScheduleSlots.AddRange(
			MakeSlot(suburbId, stage: 1, dayNumber: DayOfWeek.Monday, start: new TimeOnly(6, 0)),
			MakeSlot(suburbId, stage: 2, dayNumber: DayOfWeek.Monday, start: new TimeOnly(10, 0)),
			MakeSlot(suburbId, stage: 4, dayNumber: DayOfWeek.Monday, start: new TimeOnly(14, 0))  // should be excluded
		);
		await _db.SaveChangesAsync();

		var result = await _repo.GetBySuburbAndStageAsync(suburbId, stage: 2);

		result.Should().HaveCount(2);
		result.Should().AllSatisfy(s => s.Stage.Should().Be(2));
	}

	[Fact]
	public async Task GetBySuburbAndStageAsync_OrderedByDayThenTime()
	{
		var suburbId = await SeedSuburbAsync();
		_db.ScheduleSlots.AddRange(
			MakeSlot(suburbId, stage: 2, dayNumber: DayOfWeek.Tuesday, start: new TimeOnly(8, 0)),
			MakeSlot(suburbId, stage: 2, dayNumber: DayOfWeek.Monday, start: new TimeOnly(22, 0)),
			MakeSlot(suburbId, stage: 2, dayNumber: DayOfWeek.Monday, start: new TimeOnly(10, 0))
		);
		await _db.SaveChangesAsync();

		var result = await _repo.GetBySuburbAndStageAsync(suburbId, stage: 2);
		result[0].ScheduleDay.Should().Be(DayOfWeek.Monday);
		result[0].StartTime.Should().Be(new TimeOnly(10, 0));
		result[1].ScheduleDay.Should().Be(DayOfWeek.Monday);
		result[1].StartTime.Should().Be(new TimeOnly(22, 0));
		result[2].ScheduleDay.Should().Be(DayOfWeek.Tuesday);
	}

	[Fact]
	public async Task GetBySuburbAndStageAsync_DifferentSuburb_ReturnsEmpty()
	{
		var suburbId = await SeedSuburbAsync();
		_db.ScheduleSlots.Add(MakeSlot(suburbId, stage: 2));
		await _db.SaveChangesAsync();

		var result = await _repo.GetBySuburbAndStageAsync(suburbId + 99, stage: 2);

		result.Should().BeEmpty();
	}

	[Fact]
	public async Task GetUpcomingForSuburbAsync_ReturnsSlots_AfterGivenTime_SameDay()
	{
		var suburbId = await SeedSuburbAsync();
		_db.ScheduleSlots.AddRange(
			MakeSlot(suburbId, stage: 2, dayNumber: DayOfWeek.Monday, start: new TimeOnly(12, 0)),   // past — excluded
			MakeSlot(suburbId, stage: 2, dayNumber: DayOfWeek.Monday, start: new TimeOnly(14, 0)),   // upcoming
			MakeSlot(suburbId, stage: 2, dayNumber: DayOfWeek.Monday, start: new TimeOnly(18, 0)),   // upcoming
			MakeSlot(suburbId, stage: 2, dayNumber: DayOfWeek.Tuesday, start: new TimeOnly(14, 0))    // wrong day — excluded
		);
		await _db.SaveChangesAsync();

		var result = await _repo.GetUpcomingForSuburbAsync(
			suburbId, currentStage: 2, dayNumber: DayOfWeek.Monday, afterTime: new TimeOnly(13, 0));

		result.Should().HaveCount(2);
		result.Should().AllSatisfy(s =>
		{
			s.ScheduleDay.Should().Be(DayOfWeek.Monday);
			s.StartTime.Should().BeAfter(new TimeOnly(13, 0));
		});
	}

	[Fact]
	public async Task GetUpcomingForSuburbAsync_StageAboveCurrent_Excluded()
	{
		var suburbId = await SeedSuburbAsync();
		_db.ScheduleSlots.AddRange(
			MakeSlot(suburbId, stage: 2, dayNumber: DayOfWeek.Monday, start: new TimeOnly(16, 0)),  // included
			MakeSlot(suburbId, stage: 6, dayNumber: DayOfWeek.Monday, start: new TimeOnly(16, 30))  // stage 6 > current 4
		);
		await _db.SaveChangesAsync();

		var result = await _repo.GetUpcomingForSuburbAsync(
			suburbId, currentStage: 4, dayNumber: DayOfWeek.Monday, afterTime: new TimeOnly(15, 0));

		result.Should().ContainSingle();
		result[0].Stage.Should().Be(2);
	}

	[Fact]
	public async Task GetUpcomingForSuburbAsync_OrderedByStartTime()
	{
		var suburbId = await SeedSuburbAsync();
		_db.ScheduleSlots.AddRange(
			MakeSlot(suburbId, stage: 2, dayNumber: DayOfWeek.Wednesday, start: new TimeOnly(20, 0)),
			MakeSlot(suburbId, stage: 2, dayNumber: DayOfWeek.Wednesday, start: new TimeOnly(14, 0)),
			MakeSlot(suburbId, stage: 2, dayNumber: DayOfWeek.Wednesday, start: new TimeOnly(18, 0))
		);
		await _db.SaveChangesAsync();

		var result = await _repo.GetUpcomingForSuburbAsync(
			suburbId, currentStage: 2, dayNumber: DayOfWeek.Wednesday, afterTime: new TimeOnly(13, 0));

		result.Select(s => s.StartTime).Should().BeInAscendingOrder();
	}

	[Fact]
	public async Task DeleteBySuburbAsync_RemovesAllSlotsForSuburb()
	{
		var suburbId = await SeedSuburbAsync();
		_db.ScheduleSlots.AddRange(
			MakeSlot(suburbId, stage: 1),
			MakeSlot(suburbId, stage: 2)
		);
		await _db.SaveChangesAsync();

		await _repo.DeleteBySuburbAsync(suburbId);
		await _repo.UnitOfWork.SaveEntitiesAsync();

		(await _repo.GetBySuburbAndStageAsync(suburbId, stage: 1)).Should().BeEmpty();
		(await _repo.GetBySuburbAndStageAsync(suburbId, stage: 2)).Should().BeEmpty();
	}

	[Fact]
	public async Task DeleteBySuburbAsync_DoesNotAffectOtherSuburbs()
	{
		var s1 = await Seed.SuburbAsync(_db, 1);
		var p = await Seed.ProvinceAsync(_db, eskomId: 2);
		var m = await Seed.MunicipalityAsync(_db, p.Id);
		var s2 = await Seed.SuburbAsync(_db, m.Id);

		_db.ScheduleSlots.AddRange(
			MakeSlot(s1.EskomId, stage: 2),
			MakeSlot(s2.EskomId, stage: 2)
		);
		await _db.SaveChangesAsync();

		await _repo.DeleteBySuburbAsync(s1.EskomId);
		await _repo.UnitOfWork.SaveEntitiesAsync();

		var remaining = await _repo.GetBySuburbAndStageAsync(s2.EskomId, stage: 2);
		remaining.Should().ContainSingle();
		remaining[0].SuburbId.Should().Be(s2.EskomId);
	}
}
