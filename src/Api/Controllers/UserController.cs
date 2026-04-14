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
	private readonly IUserService _service;

	private readonly UserMapper mapper = new();

	public UserController(IUserService service)
	{
		_service = service;
	}
}