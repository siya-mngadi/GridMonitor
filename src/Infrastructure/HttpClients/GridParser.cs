using AngleSharp;
using AngleSharp.Dom;
using Microsoft.Extensions.Logging;

namespace GridMonitor.Infrastructure.HttpClients;

public class GridParser
{
	private readonly ILogger<GridParser> _logger;
	private static readonly IBrowsingContext AngleSharp = BrowsingContext.New(Configuration.Default);

	private static readonly TimeZoneInfo SAZone = TimeZoneInfo.FindSystemTimeZoneById(
			OperatingSystem.IsWindows() ? "South Africa Standard Time" : "Africa/Johannesburg"
	);

	public GridParser(ILogger<GridParser> logger) => _logger = logger;

	public async Task<SuburbSchedule> ParseScheduleAsync(
		Suburb suburb,
		int stage,
		string html,
		CancellationToken ct = default)
	{
		try
		{
			var doc = await AngleSharp.OpenAsync(req => req.Content(html), ct);

			// Eskom's schedule lives in a <ul class="list_schedule"> or a <table>
			// They have restyled a few times — try table first, then list
			var days = TryParseTable(doc, suburb, stage)
					?? TryParseList(doc, suburb, stage)
					?? [];

			if (days.Count == 0)
			{
				_logger.LogWarning("No schedule data parsed for suburb {Name}", suburb.Name);
				// Save raw HTML for debugging
				await File.WriteAllTextAsync(
					$"debug_{suburb.Id}_stage{stage}.html", html, ct);
			}

			return new SuburbSchedule(suburb, days, DateTime.UtcNow);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Parse failed for suburb {Name}", suburb.Name);
			return null;
		}
	}

	private List<DaySchedule> TryParseTable(IDocument doc, Suburb suburb, int stage)
	{
		// Try multiple selectors — Eskom has changed their HTML structure over the years
		var table = doc.QuerySelector("table.scheduleTable")
				 ?? doc.QuerySelector("table#ctl00_ContentPlaceHolder1_GridView1")
				 ?? doc.QuerySelector("table");

		if (table == null) return [];

		var headers = table.QuerySelectorAll("thead th, thead td")
						   .Skip(1)  // skip "Time" column
						   .Select(th => th.TextContent.Trim())
						   .ToList();

		if (headers.Count == 0)
		{
			// Some Eskom pages put headers in the first tbody row
			var firstRow = table.QuerySelector("tbody tr:first-child");
			if (firstRow != null)
				headers = firstRow.QuerySelectorAll("td")
								  .Skip(1)
								  .Select(td => td.TextContent.Trim())
								  .ToList();
		}

		if (headers.Count == 0)
		{
			_logger.LogDebug("Table found but no headers for {Name}", suburb.Name);
			return null;
		}

		// Initialise a slot list per day
		var daySlots = headers.Select(h => new DaySchedule(h, [])).ToList();

		// Parse each row
		var rows = table.QuerySelectorAll("tbody tr");
		foreach (var row in rows)
		{
			var cells = row.QuerySelectorAll("td").ToList();
			if (cells.Count < 2) continue;

			var timeText = cells[0].TextContent.Trim();
			if (!TryParseTimeRange(timeText, out var start, out var end))
				continue;

			// Each remaining cell corresponds to a day
			for (var i = 1; i < cells.Count && i - 1 < daySlots.Count; i++)
			{
				var cellContent = cells[i].TextContent.Trim();
				var cellClass = cells[i].GetAttribute("class") ?? "";

				// Eskom marks active slots with content like "1", "2", "x",
				// or a CSS class like "active", "stage1", "on"
				if (!IsActiveCell(cellContent, cellClass)) continue;

				daySlots[i - 1].Slots.Add(new ScheduleSlot(
					suburb.Id,
					suburb.Name,
					stage,
					start,
					end
				));
			}
		}

		return daySlots.Any(d => d.Slots.Count != 0) ? daySlots : [];
	}

	private List<DaySchedule> TryParseList(IDocument doc, Suburb suburb, int stage)
	{
		var container = doc.QuerySelector("ul.list_schedule")
					  ?? doc.QuerySelector(".schedule-container");

		if (container == null) return [];

		var days = new List<DaySchedule>();

		var dayItems = container.QuerySelectorAll("li.schedule_day, .schedule-day");
		foreach (var dayItem in dayItems)
		{
			var title = dayItem.QuerySelector(".day_title, .day-title, h3, strong")
							   ?.TextContent.Trim() ?? "Unknown";

			var slots = new List<ScheduleSlot>();
			var timeItems = dayItem.QuerySelectorAll("li.time_slot, .time-slot");

			foreach (var timeItem in timeItems)
			{
				var timeText = timeItem.TextContent.Trim();
				if (!TryParseTimeRange(timeText, out var start, out var end)) continue;

				slots.Add(new ScheduleSlot(suburb.Id, suburb.Name, stage, start, end));
			}

			days.Add(new DaySchedule(title, slots));
		}

		return days;
	}

	// Parse "00:00-02:30" or "00:00 - 02:30"
	private static bool TryParseTimeRange(
		string input, out TimeOnly start, out TimeOnly end)
	{
		start = end = default;
		var parts = input.Split('-', StringSplitOptions.TrimEntries);
		if (parts.Length != 2) return false;

		return TimeOnly.TryParse(parts[0], out start)
			&& TimeOnly.TryParse(parts[1], out end);
	}

	// Determine if a table cell represents an active (shed) slot
	private static bool IsActiveCell(string content, string cssClass)
	{
		if (string.IsNullOrWhiteSpace(content) && string.IsNullOrWhiteSpace(cssClass))
			return false;

		// Eskom uses various conventions across versions:
		// numeric content, "x" marker, or CSS classes
		var c = content.ToLower();
		var cls = cssClass.ToLower();

		return c is "x" or "1" or "yes" or "on" or "*"
			|| cls.Contains("active")
			|| cls.Contains("stage")
			|| cls.Contains("on")
			|| (!string.IsNullOrEmpty(content) && content != "&nbsp;" && content != "-");
	}

	public static (DateTime startUtc, DateTime endUtc) ToUtcSlot(
	 DateOnly date,
	 TimeOnly start,
	 TimeOnly end)
	{
		var startLocal = date.ToDateTime(start);
		// Handle midnight crossover e.g. 22:00–00:30
		var endLocal = end <= start
			? date.AddDays(1).ToDateTime(end)
			: date.ToDateTime(end);

		return (
			TimeZoneInfo.ConvertTimeToUtc(
				DateTime.SpecifyKind(startLocal, DateTimeKind.Unspecified), SAZone),
			TimeZoneInfo.ConvertTimeToUtc(
				DateTime.SpecifyKind(endLocal, DateTimeKind.Unspecified), SAZone)
		);
	}

}