using FluentAssertions;
using GridMonitor.Application.Services;
using GridMonitor.Domain.Entities;
using GridMonitor.Domain.Repositories;
using GridMonitor.Tests.Unit.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace GridMonitor.Tests.Unit.Services;

public class ScheduleServiceTests
{
	private readonly IStageSnapshotRepository _snapshots = Substitute.For<IStageSnapshotRepository>();
	private readonly IScheduleSlotRepository _slots = Substitute.For<IScheduleSlotRepository>();
	private readonly ISuburbRepository _suburbs = Substitute.For<ISuburbRepository>();
	private readonly ScheduleService _service;

	public ScheduleServiceTests()
	{
		_service = new ScheduleService(_snapshots, _slots, _suburbs, NullLogger<ScheduleService>.Instance);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(3)]
	[InlineData(8)]
	public async Task GetCurrentStage_ReturnsSnapshotValue(short stage)
	{
		_snapshots.GetCurrentStageAsync().Returns(stage);

		var result = await _service.GetCurrentStageAsync();

		result.Success.Should().BeTrue();
		result.Value.Should().Be(stage);
	}

	[Fact]
	public async Task GetCurrentStage_AlwaysSucceeds_EvenWhenZero()
	{
		_snapshots.GetCurrentStageAsync().Returns((short)0);

		var result = await _service.GetCurrentStageAsync();

		result.Success.Should().BeTrue();
		result.Value.Should().Be((short)0);
	}

	[Fact]
	public async Task GetSchedule_ExistingSuburb_ReturnsScheduleWithSlots()
	{
		var suburb = GenerateMockObjects.Suburb(1);
		var slots = new List<ScheduleSlot>
		{
			GenerateMockObjects.Slot(1, stage: 2, dayNumber: DayOfWeek.Monday, start: new TimeOnly(22, 0)),
			GenerateMockObjects.Slot(1, stage: 2, dayNumber: DayOfWeek.Monday, start: new TimeOnly(6, 0))
		};

		_suburbs.GetByIdAsync(1).Returns(suburb);
		_snapshots.GetCurrentStageAsync().Returns((short)2);
		_slots.GetBySuburbAndStageAsync(1, 2).Returns(slots);

		var result = await _service.GetScheduleAsync(1);

		result.Success.Should().BeTrue();
		result.Value!.SuburbName.Should().Be("Suburb 1");
		result.Value.CurrentStage.Should().Be(2);
		result.Value.UpcomingSlots.Should().HaveCount(2);
	}

	[Fact]
	public async Task GetSchedule_SuburbNotFound_Fails()
	{
		_suburbs.GetByIdAsync(Arg.Any<int>()).Returns(default(Suburb));

		var result = await _service.GetScheduleAsync(999);

		result.Success.Should().BeFalse();
		result.Error.Should().Contain("not found");
	}

	[Fact]
	public async Task GetSchedule_MapsSlotFieldsCorrectly()
	{
		var suburb = GenerateMockObjects.Suburb(1);
		var slot = GenerateMockObjects.Slot(1, stage: 4, dayNumber: DayOfWeek.Wednesday,
			start: new TimeOnly(10, 0), end: new TimeOnly(12, 30));

		_suburbs.GetByIdAsync(1).Returns(suburb);
		_snapshots.GetCurrentStageAsync().Returns((short)4);
		_slots.GetBySuburbAndStageAsync(1, 4).Returns([slot]);

		var result = await _service.GetScheduleAsync(1);

		var s = result.Value!.UpcomingSlots[0];
		s.Stage.Should().Be(4);
		s.ScheduleDay.Should().Be(DayOfWeek.Wednesday);
		s.StartTime.Should().Be(new TimeOnly(10, 0));
		s.EndTime.Should().Be(new TimeOnly(12, 30));
	}

	[Fact]
	public async Task GetSchedule_StageZero_ReturnsEmptySlots()
	{
		_suburbs.GetByIdAsync(1).Returns(GenerateMockObjects.Suburb());
		_snapshots.GetCurrentStageAsync().Returns((short)0);
		_slots.GetBySuburbAndStageAsync(1, 0).Returns([]);

		var result = await _service.GetScheduleAsync(1);

		result.Success.Should().BeTrue();
		result.Value!.CurrentStage.Should().Be(0);
		result.Value.UpcomingSlots.Should().BeEmpty();
	}

	// ── GetUpcomingAsync ──────────────────────────────────────────────────────

	[Fact]
	public async Task GetUpcoming_ExistingSuburb_ReturnsUpcomingSlots()
	{
		_suburbs.GetByIdAsync(1).Returns(GenerateMockObjects.Suburb());
		_slots.GetUpcomingForSuburbAsync(
			Arg.Any<int>(), Arg.Any<int>(), Arg.Any<DayOfWeek>(), Arg.Any<TimeOnly>())
			.Returns([GenerateMockObjects.Slot(1)]);

		var result = await _service.GetUpcomingAsync(1, currentStage: 2);

		result.Success.Should().BeTrue();
		result.Value.Should().HaveCount(1);
	}

	[Fact]
	public async Task GetUpcoming_SuburbNotFound_Fails()
	{
		_suburbs.GetByIdAsync(Arg.Any<int>()).Returns(default(Suburb));

		var result = await _service.GetUpcomingAsync(999, currentStage: 2);

		result.Success.Should().BeFalse();
		result.Error.Should().Contain("not found");
	}

	[Fact]
	public async Task GetUpcoming_PassesTodayDayNumberAndCurrentTime()
	{
		_suburbs.GetByIdAsync(1).Returns(GenerateMockObjects.Suburb());
		_slots.GetUpcomingForSuburbAsync(
			Arg.Any<int>(), Arg.Any<int>(), Arg.Any<DayOfWeek>(), Arg.Any<TimeOnly>())
			.Returns([]);

		await _service.GetUpcomingAsync(1, currentStage: 3);

		await _slots.Received(1).GetUpcomingForSuburbAsync(
			1,
			3,
			Arg.Is<DayOfWeek>(d => d >= DayOfWeek.Sunday && d <= DayOfWeek.Saturday),
			Arg.Is<TimeOnly>(t => t != default));      // non-zero time
	}

	[Fact]
	public async Task SearchSuburbs_ValidQuery_ReturnsResults()
	{
		_suburbs.SearchAsync("milner", 20).Returns(
		[
			GenerateMockObjects.Suburb(1), GenerateMockObjects.Suburb(2)
		]);

		var result = await _service.SearchSuburbsAsync("milner");

		result.Success.Should().BeTrue();
		result.Value.Should().HaveCount(2);
	}

	[Theory]
	[InlineData("")]
	[InlineData("  ")]
	[InlineData("a")]   // single character — below 2-char minimum
	public async Task SearchSuburbs_ShortQuery_Fails_BeforeDbCall(string query)
	{
		var result = await _service.SearchSuburbsAsync(query);

		result.Success.Should().BeFalse();
		result.Error.Should().Contain("at least 2");
		await _suburbs.DidNotReceive().SearchAsync(Arg.Any<string>(), Arg.Any<int>());
	}

	[Fact]
	public async Task SearchSuburbs_TrimsQueryBeforePassing()
	{
		_suburbs.SearchAsync("obs", 20).Returns([]);

		await _service.SearchSuburbsAsync("  obs  ");

		await _suburbs.Received(1).SearchAsync("obs", 20);
	}

	[Fact]
	public async Task SearchSuburbs_NoResults_ReturnsEmptyList()
	{
		_suburbs.SearchAsync(Arg.Any<string>(), Arg.Any<int>())
				.Returns([]);

		var result = await _service.SearchSuburbsAsync("zzznomatch");

		result.Success.Should().BeTrue();
		result.Value.Should().BeEmpty();
	}
}
