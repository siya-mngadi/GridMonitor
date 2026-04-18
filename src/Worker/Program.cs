using GridMonitor.Domain.Repositories;
using GridMonitor.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GridMonitor.Worker;

public class Program
{
	private static async Task Main(string[] args)
	{
		var builder = Host.CreateApplicationBuilder(args);

		var envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

		builder.Environment.EnvironmentName = envName;

		builder.Configuration.SetBasePath(AppContext.BaseDirectory)
			.AddJsonFile("appsettings.json")
			.AddJsonFile($"appsettings.{envName}.json", optional: true);

		builder.Services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));

		builder.Services.AddWorkerDatabase(builder.Configuration);

		if (builder.Environment.IsProduction())
		{
			builder.Configuration.ConfigureWorkerRailwayDatabase();
		}

		builder.Services.AddScoped<IMunicipalityRepository, MunicipalityRepository>();
		builder.Services.AddScoped<ISuburbRepository, SuburbRepository>();
		builder.Services.AddScoped<IProvinceRepository, ProvinceRepository>();
		builder.Services.AddScoped<ISyncRunRepository, SyncRunRepository>();
		builder.Services.AddScoped<IAlertLogRepository, AlertLogRepository>();
		builder.Services.AddScoped<IStageSnapshotRepository, StageSnapshotRepository>();
		builder.Services.AddScoped<IScheduleSlotRepository, ScheduleSlotRepository>();

		builder.Services.AddHttpClient<GridClient>();

		builder.Services.AddScoped<IGridService, GridService>();

		builder.Services.AddHostedService<GridWorker>();
		builder.Services.AddHostedService<MaintenanceWorker>();

		var host = builder.Build();

		await host.RunAsync();
	}
}