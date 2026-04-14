using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace GridMonitor.Api;

public static class ServiceProviderExtensions
{
	public static void AddGridMonitorCors(this IServiceCollection services) =>
			services.AddCors(options =>
			{
				options.AddPolicy(Program.CorsPolicyName, builder =>
				builder.AllowAnyOrigin()
				.AllowAnyMethod()
				.AllowAnyHeader());
			});

	public static void AddRateLimiting(this IServiceCollection services)
	{
		services.AddRateLimiter(options =>
		{
			options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
			options.AddFixedWindowLimiter("fixed", limiterOptions =>
			{
				limiterOptions.PermitLimit = 50;
				limiterOptions.Window = TimeSpan.FromMinutes(1);
				limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
				limiterOptions.QueueLimit = 25;
			});
		});
	}
}
