using GridMonitor.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Net;

namespace GridMonitor.Infrastructure.HttpClients;

public class GridClient
{
	private readonly HttpClient _http;
	private readonly ILogger<GridClient> _logger;

	private const string BaseUrl = "https://loadshedding.eskom.co.za/LoadShedding";

	// Impersonate browser
	private const string UserAgent =
		"Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
		"AppleWebKit/537.36 (KHTML, like Gecko) " +
		"Chrome/124.0.0.0 Safari/537.36";

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
	/// Retrieves the current load shedding status from the Eskom API.
	/// </summary>
	/// <remarks>The returned stage is normalized to 0-based indexing, where 0 indicates no load shedding, 1
	/// indicates stage 1, and so on.</remarks>
	/// <param name="ct">A cancellation token that can be used to cancel the operation.</param>
	/// <returns> The EskomStatus object representing the current load shedding stage, or null if the status could not be determined.</returns>
	public async Task<StageSnapshot> GetStatusAsync(CancellationToken ct = default)
	{
		try
		{
			var raw = await _http.GetStringAsync("GetStatus", ct);
			raw = raw.Trim().Trim('"');  // sometimes JSON-quoted

			if (!int.TryParse(raw, out var value))
			{
				_logger.LogWarning("Unexpected status response: {Raw}", raw);
				return null;
			}

			// Eskom: 1 = no shedding, 2 = stage 1, 3 = stage 2, etc.
			var stage = value <= 1 ? 0 : value - 1;

			_logger.LogInformation("Current stage: {Stage} (raw={Raw})", stage, raw);
			return new StageSnapshot
			{
				Id = stage,
				RawText = raw,
				CreatedAt = DateTime.UtcNow,
			};
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "GetStatus failed");
			return null;
		}
	}

	public async Task<string> GetScheduleHtmlAsync(Suburb suburb, int stage, CancellationToken ct = default)
	{
		if (stage < 1 || stage > 8)
			throw new ArgumentOutOfRangeException(nameof(stage), "Stage must be 1–8");

		try
		{
			var url = $"GetScheduleM/{suburb.Id}/{stage}/_/1";
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
			_logger.LogError(ex, "GetScheduleHtml failed for suburb {Id}", suburb.Id);
			return null;
		}
	}
}