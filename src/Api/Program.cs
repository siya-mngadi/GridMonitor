using Asp.Versioning;
using GridMonitor.Api.Middleware;
using GridMonitor.Application;
using GridMonitor.Infrastructure;
using Keycloak.AuthServices.Authentication;
using Keycloak.AuthServices.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Scalar.AspNetCore;
using Serilog;
using System.Text.Json.Serialization;

namespace GridMonitor.Api;

public partial class Program
{
	public const string CorsPolicyName = "CorsPolicy";
	private static void Main(string[] args)
	{
		var builder = WebApplication.CreateBuilder(args);

		Log.Logger = new LoggerConfiguration()
			.WriteTo.Console()
			.CreateBootstrapLogger();

		var envName = builder.Environment.EnvironmentName;

		builder.Configuration.SetBasePath(AppContext.BaseDirectory)
			.AddJsonFile("appsettings.json")
			.AddJsonFile($"appsettings.{envName}.json", optional: true)
			.AddJsonFile($"appsettings.{Environment.MachineName}.json", optional: true);

		// Use Kestrel as the web server
		builder.WebHost.UseKestrelCore();

		// Add Cors
		builder.Services.AddGridMonitorCors();

		// Services
		builder.Services.AddApplicationLayer(builder.Configuration);

		// Repositories and proxies
		builder.Services.AddInfrastructureLayer(builder.Configuration);

		// Add Keycloak authentication
		builder.Services.AddKeycloakWebApiAuthentication(builder.Configuration);

		builder.Services.AddKeycloakAuthorization(builder.Configuration);

		// Add distributed memory cache
		builder.Services.AddDistributedMemoryCache();

		// Add caller context 
		builder.Services.AddScoped<CallerContext>();

		builder.Services.AddControllers()
				.AddJsonOptions(options =>
				{
					options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
					options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
				});

		builder.Services.Configure<ForwardedHeadersOptions>(options =>
		{
			options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

			options.ForwardLimit = null; // No limit on the number of forwarded headers to process
			options.KnownIPNetworks.Clear();
			options.KnownProxies.Clear();
		});

		// Add Production Postgres
		if (builder.Environment.IsProduction())
		{
			builder.Configuration.ConfigureRailwayDatabaseConnectionString();
		}

		// Add services to the container.
		builder.Services.AddOpenApi();

		// Add rate limiting
		builder.Services.AddRateLimiting();

		// add Api Versioning
		builder.Services.AddApiVersioning(options =>
		{
			options.AssumeDefaultVersionWhenUnspecified = true;
			options.DefaultApiVersion = new ApiVersion(1, 0);
			options.ReportApiVersions = true;
			options.ApiVersionReader = ApiVersionReader.Combine(new UrlSegmentApiVersionReader(),
				new HeaderApiVersionReader("X-Api-Version"));
		}).AddApiExplorer(options =>
		{
			options.GroupNameFormat = "'v'VVV";
			options.SubstituteApiVersionInUrl = true;
			options.DefaultApiVersion = new ApiVersion(1, 0);
		});

		// Serilog configuration
		builder.Host.UseSerilog((context, config) =>
		{
			config.ReadFrom.Configuration(context.Configuration);
		});

		var app = builder.Build();

		app.UseForwardedHeaders();

		if (app.Environment.IsDevelopment())
		{
			app.MapOpenApi();
			app.MapScalarApiReference();
		}
		else
		{
			app.UseHsts();
		}

		app.UseHttpsRedirection();

		app.UseAuthentication();

		app.UseAuthorization();

		app.UseMiddleware<AuthMiddleware>();

		app.UseMiddleware<ExceptionMiddleware>();

		app.UseCors(CorsPolicyName);

		app.MapControllers();

		app.Run();
	}
}