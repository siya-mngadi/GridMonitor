using Asp.Versioning;
using GridMonitor.Api.Mappers;
using GridMonitor.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GridMonitor.Api.Controllers;

[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users")]
public class UserController : ControllerBase
{
	private readonly IUserService userService;
	private readonly IUsageService usageService;
	public GridMapper mapper = new();
	public UserController(IUserService userService, IUsageService usageService)
	{
		this.userService = userService;
		this.usageService = usageService;
	}

	[HttpGet]
	public async ValueTask<IActionResult> GetUserInfo()
	{
		var userId = HttpContext.Items["UserId"] as string;
		if (!Guid.TryParse(userId, out var userGuid))
			return Unauthorized();

		var result = await userService.GetByIdAsync(userGuid);
		if (!result.Success)
			return BadRequest(new { error = result.Error });

		return Ok(mapper.ToResponse(result.Value!));
	}

	[HttpGet]
	public async Task<IActionResult> GetUsage()
	{
		var userId = HttpContext.Items["UserId"] as string;
		if (!Guid.TryParse(userId, out var userGuid))
			return Unauthorized();

		var statsResult = await usageService.GetStatsAsync(userGuid);
		return Ok(statsResult);
	}
}