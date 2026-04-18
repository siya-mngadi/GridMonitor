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
public class ApiKeyController : ControllerBase
{
	private readonly IApiKeyService apiKeyService;
	private GridMapper mapper = new();
	public ApiKeyController(IApiKeyService apiKeyService)
	{
		this.apiKeyService = apiKeyService;
	}

	[HttpGet]
	public async ValueTask<IActionResult> GetApiKeys()
	{
		var userId = HttpContext.Items["UserId"] as string;
		if (!Guid.TryParse(userId, out var userGuid))
			return Unauthorized();
		var result = await apiKeyService.GetApiKeysAsync(userGuid);
		
		if (!result.Success)
			return BadRequest(new { error = result.Error });
		var items = mapper.ToResponse(result.Value!);
		return Ok(new { results = items, count = result.Value.Count });
	}

	[HttpPost]
	public async ValueTask<IActionResult> CreateApiKey()
	{
		var userId = HttpContext.Items["UserId"] as string;
		if (!Guid.TryParse(userId, out var userGuid))
			return Unauthorized();
		var result = await apiKeyService.IssueAsync(userGuid);
		
		if (!result.Success)
			return BadRequest(new { error = result.Error });
		
		return Ok(result.Value);
	}

	[HttpPost]
	public async ValueTask<IActionResult> RegenerateApiKey([FromQuery] Guid apiKey)
	{
		var userId = HttpContext.Items["UserId"] as string;
		if (!Guid.TryParse(userId, out var userGuid))
			return Unauthorized();
		var result = await apiKeyService.RotateAsync(apiKey, userGuid);

		if (!result.Success)
			return BadRequest(new { error = result.Error });
		
		return Ok(result.Value);
	}

	[HttpDelete]
	public async ValueTask<IActionResult> RevokeApiKey([FromQuery] Guid apiKey)
	{
		var userId = HttpContext.Items["UserId"] as string;
		if (!Guid.TryParse(userId, out var userGuid))
			return Unauthorized();
		var result = await apiKeyService.RevokeAsync(apiKey, userGuid);

		if (!result.Success)
			return BadRequest(new { error = result.Error });
		
		return NoContent();
	}
}
