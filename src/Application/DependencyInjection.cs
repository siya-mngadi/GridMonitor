using GridMonitor.Application.Engine;
using GridMonitor.Application.Services;
using GridMonitor.Application.Workers;
using GridMonitor.Domain.Services;
using GridMonitor.Infrastructure.Proxies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GridMonitor.Application;

public static class DependencyInjection
{
	public static IServiceCollection AddApplicationLayer(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddConfiguration(configuration);
		services.AddServices();
		services.AddHostedServices();
		services.AddProxyClients();
		services.AddScoped<AlertEngine>();
		return services;
	}

	public static IServiceCollection AddProxyClients(this IServiceCollection services)
	{
		services.AddHttpClient<GridClient>();
		return services;
	}

	public static IServiceCollection AddServices(this IServiceCollection services)
	{
		services.AddScoped<IUserService, UserService>();
		services.AddScoped<IApiKeyService, ApiKeyService>();
		services.AddScoped<ISubscriptionService, SubscriptionService>();
		services.AddScoped<IScheduleService, ScheduleService>();
		services.AddScoped<IStageService, StageService>();
		services.AddScoped<IUsageService, UsageService>();
		return services;
	}

	public static IServiceCollection AddHostedServices(this IServiceCollection services)
	{
		services.AddHostedService<SchedulePollWorker>();
		services.AddHostedService<StagePollWorker>();
		services.AddHostedService<AlertCheckWorker>();
		return services;
	}


	public static IServiceCollection AddConfiguration(this IServiceCollection services, IConfiguration configuration)
	{
		//services.Configure<Authentication>(configuration.GetSection(nameof(Authentication)));
		//services.Configure<ShortCodeConfig>(configuration.GetSection(nameof(ShortCodeConfig)));
		//services.Configure<ConnectionStrings>(configuration.GetSection(nameof(ConnectionStrings)));
		//services.Configure<IPlocatorConfig>(configuration.GetSection(nameof(IPlocatorConfig)));
		return services;
	}
}
