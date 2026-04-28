using FluentAssertions;
using GridMonitor.Application.Services;
using GridMonitor.Domain.Entities;
using GridMonitor.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace GridMonitor.Tests.Unit.Services;

public class StageServiceTests
{
	private readonly IStageSnapshotRepository _snapshots = Substitute.For<IStageSnapshotRepository>();
	private readonly StageService _service;

	public StageServiceTests()
	{
		_service = new StageService(_snapshots, NullLogger<StageService>.Instance);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(4)]
	[InlineData(8)]
	public async Task RecordStage_ValidRange_Succeeds(short stage)
	{
		_snapshots.GetCurrentStageAsync().Returns((short)0);

		var result = await _service.RecordStageAsync(stage, stage.ToString());

		result.Success.Should().BeTrue();
		await _snapshots.Received(1).UpdateAsync(
			Arg.Is<StageSnapshot>(s => s.Stage == stage && s.RawText == stage.ToString()));
		await _snapshots.Received(1).UnitOfWork.SaveEntitiesAsync();
	}

	[Theory]
	[InlineData(-1)]
	[InlineData(9)]
	[InlineData(100)]
	[InlineData(-8)]
	public async Task RecordStage_OutOfRange_Fails_NothingSaved(short stage)
	{
		var result = await _service.RecordStageAsync(stage, stage.ToString());

		result.Success.Should().BeFalse();
		result.Error.Should().Contain("0–8");
		await _snapshots.DidNotReceive().UpdateAsync(Arg.Any<StageSnapshot>());
	}

	[Fact]
	public async Task RecordStage_StageUnchanged_ReturnsChangedFalse()
	{
		_snapshots.GetCurrentStageAsync().Returns((short)3);

		var result = await _service.RecordStageAsync(3, "4");

		result.Success.Should().BeTrue();
		result.Value.Should().BeFalse("same stage — no change");
	}

	[Fact]
	public async Task RecordStage_StageIncreased_ReturnsChangedTrue()
	{
		_snapshots.GetCurrentStageAsync().Returns((short)2);

		var result = await _service.RecordStageAsync(4, "5");

		result.Success.Should().BeTrue();
		result.Value.Should().BeTrue("stage went up — changed");
	}

	[Fact]
	public async Task RecordStage_StageDecreased_ReturnsChangedTrue()
	{
		_snapshots.GetCurrentStageAsync().Returns((short)6);

		var result = await _service.RecordStageAsync(2, "3");

		result.Success.Should().BeTrue();
		result.Value.Should().BeTrue("stage went down — also a change");
	}

	[Fact]
	public async Task RecordStage_FromZeroToNonZero_ReturnsChangedTrue()
	{
		_snapshots.GetCurrentStageAsync().Returns((short)0);

		var result = await _service.RecordStageAsync(2, "3");

		result.Success.Should().BeTrue();
		result.Value.Should().BeTrue();
	}

	[Fact]
	public async Task RecordStage_StageZeroWhenAlreadyZero_ReturnsChangedFalse()
	{
		_snapshots.GetCurrentStageAsync().Returns((short)0);

		var result = await _service.RecordStageAsync(0, "1");

		result.Value.Should().BeFalse();
	}

	[Fact]
	public async Task RecordStage_AlwaysPersistsSnapshot_EvenWhenUnchanged()
	{
		_snapshots.GetCurrentStageAsync().Returns((short)2);

		await _service.RecordStageAsync(2, "3"); // unchanged

		await _snapshots.Received(1).UpdateAsync(Arg.Any<StageSnapshot>());
		await _snapshots.Received(1).UnitOfWork.SaveEntitiesAsync();
	}

	[Fact]
	public async Task PurgeOldSnapshots_DelegatesWithSevenDayWindow()
	{
		await _service.PurgeOldSnapshotsAsync();

		await _snapshots.Received(1).PurgeOlderThanAsync(TimeSpan.FromDays(7));
	}
}
