using GridMonitor.Domain.Repositories;
using GridMonitor.Infrastructure.DataContext;
using GridMonitor.Infrastructure.Proxies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GridMonitor.Infrastructure;

public static class DependencyInjection
{
	public static IServiceCollection AddInfrastructureLayer(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddScopedRepositoriesFromAssembly();
		services.AddDatabase(configuration);
		services.AddWorkerServices();
		return services;
	}

	public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddDbContext<AppDbContext>(options =>
		{
			options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"), serverOptions =>
			{
				serverOptions.MigrationsAssembly(typeof(DependencyInjection).Assembly.FullName);
			});
		});
		return services;
	}

	public static void ConfigureRailwayDatabaseConnectionString(this IConfiguration configuration)
	{
		var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
		if (string.IsNullOrEmpty(databaseUrl))
		{
			throw new InvalidOperationException("DATABASE_URL environment variable is not set.");
		}
		var databaseUri = new Uri(databaseUrl);
		var userInfo = databaseUri.UserInfo.Split(':');
		var builder = new Npgsql.NpgsqlConnectionStringBuilder
		{
			Host = databaseUri.Host,
			Port = databaseUri.Port,
			Username = userInfo[0],
			Password = userInfo[1],
			Database = databaseUri.AbsolutePath.TrimStart('/'),
			SslMode = Npgsql.SslMode.Require,
		};
		configuration.GetSection("ConnectionStrings")["DefaultConnection"] = builder.ToString();
	}

	public static IServiceCollection AddScopedRepositoriesFromAssembly(this IServiceCollection services)
	{
		var assembly = typeof(DependencyInjection).Assembly;

		var implementations = assembly.GetTypes()
			.Where(t => t.IsClass && !t.IsAbstract);

		foreach (var impl in implementations)
		{
			var interfaces = impl.GetInterfaces();

			var repositoryInterfaces = interfaces
				.Where(i => i != typeof(IRepository) && typeof(IRepository).IsAssignableFrom(i))
				.ToList();

			foreach (var repoInterface in repositoryInterfaces)
			{
				services.AddScoped(repoInterface, impl);
			}
		}
		return services;
	}

	private static IServiceCollection AddWorkerServices(this IServiceCollection services)
	{
		// services.AddHostedService<AnalyticsWorker>();
		return services;
	}
}
