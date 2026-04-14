using GridMonitor.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace GridMonitor.Worker;

public static class DbExtensions
{
	public static IServiceCollection AddWorkerDatabase(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddDbContext<AppDbContext>(options =>
		{
			options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
		});
		return services;
	}

	public static void ConfigureWorkerRailwayDatabase(this IConfiguration configuration)
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
}
