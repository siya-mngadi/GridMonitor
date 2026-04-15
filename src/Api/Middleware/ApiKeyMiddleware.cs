using GridMonitor.Application.Helpers;
using GridMonitor.Domain.Services;

namespace GridMonitor.Api.Middleware;

public class ApiKeyMiddleware
{
	private readonly RequestDelegate next;
	public ApiKeyMiddleware(RequestDelegate next)
	{
		this.next = next;
	}

	public async Task InvokeAsync(HttpContext context)
	{
		// Public routes bypass auth entirely
		if (IsPublicRoute(context.Request.Path))
		{
			await next(context);
			return;
		}

		var keyService = context.RequestServices.GetRequiredService<IApiKeyService>();
		var usageService = context.RequestServices.GetRequiredService<IUsageService>();
		ApiKeyContext keyContext = null;

		// Accept key from header or query param (query only for dev convenience)
		var rawKey = context.Request.Headers["X-API-Key"].FirstOrDefault()
				  ?? context.Request.Query["api_key"].FirstOrDefault();

		if (string.IsNullOrWhiteSpace(rawKey))
		{
			context.Response.StatusCode = 401;
			await context.Response.WriteAsJsonAsync(new { error = "API key required. Pass X-API-Key header." });
			return;
		}

		var apiKeyResult = await keyService.ValidateAsync(rawKey);
		if (apiKeyResult.Value is null)
		{
			context.Response.StatusCode = 401;
			await context.Response.WriteAsJsonAsync(new { error = "Invalid API key." });
			return;
		}

		keyContext.Key = apiKeyResult.Value;

		// Rate limit check — returns false when daily quota is exceeded
		var usageResult = await usageService.CheckAndIncrementAsync(apiKeyResult.Value.Id);
		if (!usageResult.Value)
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
				resets_at = DateTime.UtcNow.Date.AddDays(1).ToString("o")
			});
			return;
		}

		await next(context);
	}

	private static bool IsPublicRoute(PathString path)
	{
		var p = path.Value ?? "";
		return p.StartsWith("/health")
			|| p.StartsWith("/scalar");
	}

	private static long NextMidnightUtcEpoch()
	{
		return new DateTimeOffset(DateTime.UtcNow.Date.AddDays(1)).ToUnixTimeSeconds();
	}
}
