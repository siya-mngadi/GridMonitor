using GridMonitor.Domain.Enums;
using GridMonitor.Domain.Repositories;
using GridMonitor.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Serilog.Context;
using System.Security.Claims;

namespace GridMonitor.Api.Middleware;

public class AuthMiddleware
{
	private readonly RequestDelegate _next;
	private readonly ILogger<AuthMiddleware> _logger;
	public AuthMiddleware(RequestDelegate next, ILogger<AuthMiddleware> logger)
	{
		_next = next;
		_logger = logger;
	}

	public async Task InvokeAsync(HttpContext context)
	{
		var endpoint = context.GetEndpoint();
		var allowAnonymous = endpoint?.Metadata?.GetMetadata<IAllowAnonymous>();

		if (allowAnonymous != null || IsPublicRoute(context.Request.Path))
		{
			using (LogContext.PushProperty("UserName", "anonymous"))
			{
				await _next(context);
			}
			return;
		}

		CallerContext caller = context.RequestServices.GetRequiredService<CallerContext>();

		// Accept key from header or query param
		var rawKey = context.Request.Headers["X-API-Key"].FirstOrDefault()
				  ?? context.Request.Query["api_key"].FirstOrDefault();

		// JWT path
		if (context.User.Identity?.IsAuthenticated ?? false)
		{
			var keycloakId = context.User.FindFirstValue("sub");
			if (string.IsNullOrEmpty(keycloakId))
			{
				await Reject(context, 401, "Valid Authenticated credentials required.");
				return;
			}

			var userRepo = context.RequestServices.GetRequiredService<IUserRepository>();
			var user = await userRepo.GetByKeycloakIdAsync(keycloakId);

			if (user is null)
			{
				await Reject(context, 401, "No valid account found for this user. Please complete sign up.");
				return;
			}

			if (!user.Active)
			{
				await Reject(context, 403, "Account is deactivated.");
				return;
			}

			caller.UserId = user.Id;
			caller.Tier = user.Tier;
			caller.IsJwt = true;
			caller.KeycloakId = keycloakId;
		}
		else if (!string.IsNullOrWhiteSpace(rawKey))
		{
			var keyService = context.RequestServices.GetRequiredService<IApiKeyService>();
			var usageService = context.RequestServices.GetRequiredService<IUsageService>();

			var apiKeyResult = await keyService.ValidateAsync(rawKey);
			if (apiKeyResult.Value is null)
			{
				await Reject(context, 401, "Invalid or revoked API key.");
				return;
			}

			var allowedResult = await usageService.CheckAndIncrementAsync(apiKeyResult.Value.Id);
			if (!allowedResult.Value)
			{
				var statsResult = await usageService.GetStatsAsync(apiKeyResult.Value.UserId);
				context.Response.StatusCode = 429;
				context.Response.Headers["X-RateLimit-Limit"] = statsResult.Value.DailyLimit.ToString();
				context.Response.Headers["X-RateLimit-Remaining"] = "0";
				context.Response.Headers["X-RateLimit-Reset"] = NextMidnightUtcEpoch().ToString();
				await context.Response.WriteAsJsonAsync(new
				{
					error = "Daily API limit reached.",
					limit = statsResult.Value.DailyLimit,
					resets_at = DateTime.UtcNow.Date.AddDays(1).ToString("o"),
					upgrade = "Upgrade your plan to increase your limit."
				});
				return;
			}

			caller.UserId = apiKeyResult.Value.UserId;
			caller.Tier = apiKeyResult.Value.User?.Tier ?? PricingTier.Free;
			caller.IsApiKey = true;
			caller.ApiKeyId = apiKeyResult.Value.Id;
		}
		else
		{
			// No credential 
			await Reject(context, 401, "Authentication required. Please sign up");
			return;
		}

		var username = context.User?.Identity?.Name ?? caller?.UserId.ToString() ?? "anonymous";
		using (LogContext.PushProperty("UserName", username))
		{
			await _next(context);
		}
	}

	private static Task Reject(HttpContext context, int status, string error)
	{
		context.Response.StatusCode = status;
		return context.Response.WriteAsJsonAsync(new { error });
	}

	private static bool IsPublicRoute(string path)
	{
		return path.StartsWith("/health")
			|| path.StartsWith("/scalar")
			|| path.StartsWith("/openapi");
	}

	private static long NextMidnightUtcEpoch()
	{
		return new DateTimeOffset(DateTime.UtcNow.Date.AddDays(1)).ToUnixTimeSeconds();
	}
}
