using Asp.Versioning;
using GridMonitor.Api.Mappers;
using GridMonitor.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GridMonitor.Api.Controllers;

[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}")]
public class ScheduleController : ControllerBase
{
	private readonly IScheduleService scheduleService;
	private readonly GridMapper mapper = new();
	public ScheduleController(IScheduleService scheduleService)
	{
		this.scheduleService = scheduleService;
	}

	[HttpGet("status")]
	public async ValueTask<IActionResult> GetCurrentStage()
	{
		var result = await scheduleService.GetCurrentStageAsync();
		if (!result.Success)
			return BadRequest(new { error = result.Error });

		return Ok(mapper.ToResponse(result.Value));
	}

	[HttpGet("areas")]
	public async ValueTask<IActionResult> GetSchedule([FromQuery] string search)
	{
		var result = await scheduleService.SearchSuburbsAsync(search);
		if (!result.Success)
			return BadRequest(new { error = result.Error });

		var items = mapper.ToResponse(result.Value!);
		return Ok(new { results =  items, count = result.Value.Count });
	}

	[HttpGet("schedule/{suburbId:int}")]
	public async ValueTask<IActionResult> GetSchedule(int suburbId)
	{
		var result = await scheduleService.GetScheduleAsync(suburbId);
		if (!result.Success)
			return NotFound(new { error = result.Error });

		return Ok(mapper.ToResponse(result.Value!));
	}

	[HttpGet("schedule/{suburbId:int}/upcoming")]
	public async ValueTask<IActionResult> GetUpcomingSchedule(int suburbId)
	{
		var stageResult = await scheduleService.GetCurrentStageAsync();
		var stage = stageResult.Value;

		if (stage == 0)
			return Ok(mapper.ToResponse(stage));

		var result = await scheduleService.GetUpcomingAsync(suburbId, stage);
		if (!result.Success)
			return NotFound(new { error = result.Error });

		return Ok(mapper.ToResponse(result.Value!));
	}
}
