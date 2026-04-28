using Asp.Versioning;
using GridMonitor.Api.Mappers;
using GridMonitor.Api.Requests;
using GridMonitor.Api.Requests.Validators;
using GridMonitor.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GridMonitor.Api.Controllers;

[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/subscriptions")]
public class SubscriptionController : ControllerBase
{
	private readonly ISubscriptionService subscriptionService;
	private readonly GridMapper mapper = new();

	private readonly SubscribeRequestValidator subscribeValidator = new();
	private readonly UpdateAlertWindowValidator updateAlertWindowValidator = new();
	private readonly AddChannelRequestValidator addChannelValidator = new();
	public SubscriptionController(ISubscriptionService subscriptionService)
	{
		this.subscriptionService = subscriptionService;
	}

	[HttpGet]
	public async ValueTask<IActionResult> GetSubscriptions()
	{
		var userId = HttpContext.Items["UserId"] as string;
		if (!Guid.TryParse(userId, out var userGuid))
			return Unauthorized();

		var result = await subscriptionService.GetUserSubscriptionsAsync(userGuid);
		if (!result.Success)
			return BadRequest(new { error = result.Error });

		var items = mapper.ToResponse(result.Value!);
		return Ok(new { results = items, count = result.Value.Count });
	}

	[HttpPost]
	public async ValueTask<IActionResult> Subscribe([FromBody] SubscribeRequest request)
	{
		var validationResult = subscribeValidator.Validate(request);
		if (!validationResult.IsValid)
			return BadRequest(new { errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)) });

		var userId = HttpContext.Items["UserId"] as string;
		if (!Guid.TryParse(userId, out var userGuid))
			return Unauthorized();

		var details = mapper.FromRequest(request);

		var result = await subscriptionService.SubscribeAsync(userGuid, details.SuburbId, details.AlertMinutesBefore);
		if (!result.Success)
			return BadRequest(new { error = result.Error });

		return Ok(mapper.ToResponse(result.Value!));
	}

	[HttpDelete("{id:guid}")]
	public async ValueTask<IActionResult> Unsubscribe(Guid id)
	{
		var userId = HttpContext.Items["UserId"] as string;
		if (!Guid.TryParse(userId, out var userGuid))
			return Unauthorized();

		var result = await subscriptionService.UnsubscribeAsync(userGuid, id);
		if (!result.Success)
			return BadRequest(new { error = result.Error });
		return Ok();
	}

	[HttpPatch("{id:guid}/alert-window")]
	public async ValueTask<IActionResult> UpdateAlertWindow(Guid id, [FromBody] UpdateAlertWindowRequest request)
	{
		var validationResult = updateAlertWindowValidator.Validate(request);
		if (!validationResult.IsValid)
			return BadRequest(new { errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)) });

		var userId = HttpContext.Items["UserId"] as string;
		if (!Guid.TryParse(userId, out var userGuid))
			return Unauthorized();

		var result = await subscriptionService.UpdateAlertWindowAsync(userGuid, id, request.AlertMinutesBefore);
		if (!result.Success)
			return BadRequest(new { error = result.Error });

		return Ok();
	}

	[HttpPost("{id:guid}/channels")]
	public async ValueTask<IActionResult> AddChannel(Guid id, [FromBody] AddChannelRequest request)
	{
		var validationResult = addChannelValidator.Validate(request);
		if (!validationResult.IsValid)
			return BadRequest(new { errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)) });

		var userId = HttpContext.Items["UserId"] as string;
		if (!Guid.TryParse(userId, out var userGuid))
			return Unauthorized();

		var details = mapper.FromRequest(request);
		var result = await subscriptionService.AddChannelAsync(userGuid, id, details.ChannelType, details.Destination);
		if (!result.Success)
			return UnprocessableEntity(new { error = result.Error });

		return CreatedAtAction(nameof(GetSubscriptions), new { id = result.Value!.Id }, mapper.ToResponse(result.Value!));
	}

	[HttpDelete("channels/{channelId:guid}")]
	public async ValueTask<IActionResult> RemoveChannel(Guid channelId)
	{
		var userId = HttpContext.Items["UserId"] as string;
		if (!Guid.TryParse(userId, out var userGuid))
			return Unauthorized();

		var result = await subscriptionService.RemoveChannelAsync(userGuid, channelId);
		if (!result.Success)
			return BadRequest(new { error = result.Error });

		return NoContent();
	}
}