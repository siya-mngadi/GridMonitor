using GridMonitor.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GridMonitor.Worker;

internal record EskomMunicipalityRaw(
  [property: JsonPropertyName("text")] string Text,
  [property: JsonPropertyName("value")] string Value);

internal record EskomSuburbPage(
	[property: JsonPropertyName("Total")] int Total,
	[property: JsonPropertyName("Results")] List<EskomSuburbRaw> Results
);

internal record EskomSuburbRaw(
	[property: JsonPropertyName("id")] string Id,
	[property: JsonPropertyName("text")] string Text,
	[property: JsonPropertyName("Tot")] int Tot
);

internal record EskomSuburbSearchRaw(
	[property: JsonPropertyName("Id")] int Id,
	[property: JsonPropertyName("MunicipalityName")] string MunicipalityName,
	[property: JsonPropertyName("Name")] string Name,
	[property: JsonPropertyName("ProvinceName")] string ProvinceName,
	[property: JsonPropertyName("Total")] int Total
);

internal class GridClient
{
	private readonly HttpClient _http;
	private readonly ILogger<GridClient> _logger;

	private const string BaseUrl = "https://loadshedding.eskom.co.za/LoadShedding";

	// Eskom returns 403 for default .NET HttpClient UA — must impersonate browser
	private const string UserAgent =
		"Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
		"AppleWebKit/537.36 (KHTML, like Gecko) " +
		"Chrome/124.0.0.0 Safari/537.36";

	private static readonly JsonSerializerOptions JsonOpts = new()
	{
		PropertyNameCaseInsensitive = true
	};

	public GridClient(HttpClient http, ILogger<GridClient> logger)
	{
		_http = http;
		_logger = logger;
		_http.BaseAddress = new Uri(BaseUrl + "/");
		_http.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
		_http.DefaultRequestHeaders.Add("Accept", "application/json, text/html, */*");
		_http.DefaultRequestHeaders.Add("Referer", "https://loadshedding.eskom.co.za/");
		_http.Timeout = TimeSpan.FromSeconds(60);
	}

	/// <summary>
	/// Retrieves a list of municipalities for the specified province.
	/// </summary>
	/// <param name="province">The province for which to retrieve municipalities.</param>
	/// <param name="ct">A cancellation token that can be used to cancel the operation.</param>
	/// <returns>A list of municipalities in the specified province. Returns an empty list if no municipalities are found or if the
	/// operation fails.</returns>
	internal async Task<List<Municipality>> GetMunicipalitiesAsync(Province province, CancellationToken ct = default)
	{
		try
		{
			var url = $"GetMunicipalities?id={province.EskomId}";
			var json = await _http.GetStringAsync(url, ct);
			var raw = JsonSerializer.Deserialize<List<EskomMunicipalityRaw>>(json, JsonOpts);

			// new Municipality(r.Value, r.Text, 0)

			return raw?.Select(r => new Municipality
			{
				EskomId = int.Parse(r.Value),
				Name = r.Text,
				Province = province,
				ProvinceId = province.EskomId,
				LastSyncedAt = DateTime.UtcNow,
				Total = 0
			}).ToList() ?? [];
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "GetMunicipalities failed for province {P}", province);
			return [];
		}
	}

	internal async Task<List<Suburb>> GetSuburbsAsync(
	Municipality municipality,
	CancellationToken ct = default)
	{
		var all = new List<Suburb>();
		var page = 1;
		const int pageSize = 100;

		while (true)
		{
			try
			{
				var url = $"GetSurburbData/?pageSize={pageSize}&pageNum={page}" +
						  $"&id={municipality.EskomId}";

				var json = await _http.GetStringAsync(url, ct);
				var raw = JsonSerializer.Deserialize<EskomSuburbPage>(json, JsonOpts);

				if (raw?.Results == null || raw.Results.Count == 0) break;

				// new Suburb(r.Id, r.Text, municipality.Id, r.Tot)
				all.AddRange(raw.Results.Select(r =>
				 new Suburb
				 {
					 EskomId = int.Parse(r.Id),
					 Name = r.Text,
					 Municipality = municipality,
					 MunicipalityId = municipality.EskomId,
					 Total = r.Tot,
					 LastSyncedAt = DateTime.UtcNow
				 }
				));

				_logger.LogDebug("Fetched page {Page} for {Municipality}: {Count} suburbs",
					page, municipality.Name, raw.Results.Count);

				// No more pages
				if (all.Count >= raw.Total || raw.Results.Count < pageSize) break;

				page++;
				await Task.Delay(5000, ct); // 5 second delay to avoid overwhelming the API
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GetSuburbs failed at page {Page} for {Municipality}",
					page, municipality.Name);
				break;
			}
		}

		return all;
	}

	internal async Task<string> GetScheduleHtmlAsync(
	int stage,
	Suburb suburb,
	Province province,
	CancellationToken ct = default)
	{
		if (stage < 1 || stage > 8)
			throw new ArgumentOutOfRangeException(nameof(stage), "Stage must be 1–8");

		try
		{
			var url = $"GetScheduleM/{suburb.EskomId}/{stage}/{province.EskomId}/{suburb.Total}";
			_logger.LogDebug("Fetching schedule: {Url}", url);

			var html = await _http.GetStringAsync(url, ct);
			return html;
		}
		catch (HttpRequestException ex) when (
			ex.StatusCode == HttpStatusCode.NotFound ||
			ex.StatusCode == HttpStatusCode.BadRequest)
		{
			_logger.LogWarning("No schedule found for suburb {Id} stage {Stage}",
				suburb.Id, stage);
			return null;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "GetSchedule Html failed for suburb {Id}", suburb.Id);
			return null;
		}
	}
}