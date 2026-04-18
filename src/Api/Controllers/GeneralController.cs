using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GridMonitor.Api.Controllers;

[AllowAnonymous]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}")]
public class GeneralController : ControllerBase
{
	public GeneralController() { } 

	[Route("health")]
	public IActionResult GetHealth()
	{
		return Ok(new { status = "Ok", ts = DateTime.UtcNow });
	}
}
