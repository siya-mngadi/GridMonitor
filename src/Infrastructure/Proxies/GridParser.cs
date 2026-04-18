using AngleSharp;
using AngleSharp.Dom;
using GridMonitor.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace GridMonitor.Infrastructure.Proxies;

public class GridParser(ILogger logger)
{
	private readonly ILogger logger = logger;

	private static readonly IBrowsingContext AngleSharp = BrowsingContext.New(Configuration.Default);

	private static readonly TimeZoneInfo SAZone =
		TimeZoneInfo.FindSystemTimeZoneById(
			OperatingSystem.IsWindows()
				? "South Africa Standard Time"
				: "Africa/Johannesburg"
		);

	public async Task<List<ScheduleSlot>> ParseScheduleAsync(
		Suburb suburb,
		short stage,
		string html,
		CancellationToken ct = default)
	{
		try
		{
			var doc = await AngleSharp.OpenAsync(req => req.Content(html), ct);

			var slots =
				TryParseDivSchedule(doc, suburb.EskomId, stage)
				?? TryParseTable(doc, suburb.EskomId, stage)
				?? TryParseList(doc, suburb.EskomId, stage)
				?? [];

			if (slots.Count == 0)
			{
				logger.LogWarning("No schedule data parsed for suburb {Name}", suburb.Name);

				await File.WriteAllTextAsync(
					$"debug_{suburb.EskomId}_stage{stage}.html",
					html,
					ct);
			}

			// Clean data
			slots = DeduplicateSlots(slots);
			slots = MergeOverlappingSlots(slots);

			return slots;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Parse failed for suburb {Name}", suburb.Name);
			return [];
		}
	}

	private List<ScheduleSlot> TryParseDivSchedule(
		IDocument doc,
		int suburbId,
		short stage)
	{
		var result = new List<ScheduleSlot>();

		var days = doc.QuerySelectorAll(".scheduleDay");
		if (days.Length == 0) return result;

		foreach (var day in days)
		{
			var dateText = day.QuerySelector(".dayMonth")?.TextContent.Trim();
			if (string.IsNullOrWhiteSpace(dateText) || dateText == "-")
				continue;

			if (!TryParseDate(dateText, out var date))
				continue;

			var links = day.QuerySelectorAll("a");
			if (!links.Any()) continue;

			foreach (var link in links)
			{
				var timeText = link.TextContent.Trim();

				if (!TryParseTimeRange(timeText, out var start, out var end))
					continue;

				var (startUtc, endUtc) = ToUtcSlot(date, start, end);

				result.Add(new ScheduleSlot
				{
					SuburbId = suburbId,
					Stage = stage,
					StartTime = TimeOnly.FromDateTime(startUtc),
					EndTime = TimeOnly.FromDateTime(endUtc),
					ScheduleDay = date.DayOfWeek,
					DataHash = $"{suburbId}_{stage}_{startUtc:O}_{endUtc:O}",
					CreatedAt = DateTime.UtcNow
				});
			}
		}

		return result;
	}

	// Optional fallback (older Eskom layouts)
	private List<ScheduleSlot> TryParseTable(
		IDocument doc,
		int suburbId,
		short stage)
	{
		var table = doc.QuerySelector("table");
		if (table == null) return [];

		var result = new List<ScheduleSlot>();

		var rows = table.QuerySelectorAll("tbody tr");
		foreach (var row in rows)
		{
			var cells = row.QuerySelectorAll("td").ToList();
			if (cells.Count < 2) continue;

			var timeText = cells[0].TextContent.Trim();
			if (!TryParseTimeRange(timeText, out var start, out var end))
				continue;

			for (int i = 1; i < cells.Count; i++)
			{
				var content = cells[i].TextContent.Trim();
				var cls = cells[i].GetAttribute("class") ?? "";

				if (!IsActiveCell(content, cls)) continue;

				// fallback: assume today (rarely used)
				var date = DateOnly.FromDateTime(DateTime.Now);

				var (startUtc, endUtc) = ToUtcSlot(date, start, end);

				result.Add(new ScheduleSlot
				{
					SuburbId = suburbId,
					Stage = stage,
					StartTime = TimeOnly.FromDateTime(startUtc),
					EndTime = TimeOnly.FromDateTime(endUtc),
					ScheduleDay = date.DayOfWeek,
					DataHash = $"{suburbId}_{stage}_{startUtc:O}_{endUtc:O}",
					CreatedAt = DateTime.UtcNow
				});
			}
		}

		return result;
	}

	private List<ScheduleSlot> TryParseList(
		IDocument doc,
		int suburbId,
		short stage)
	{
		var container = doc.QuerySelector("ul.list_schedule");
		if (container == null) return null;

		var result = new List<ScheduleSlot>();

		var items = container.QuerySelectorAll("li");
		foreach (var item in items)
		{
			var text = item.TextContent.Trim();

			if (!TryParseTimeRange(text, out var start, out var end))
				continue;

			var date = DateOnly.FromDateTime(DateTime.Now);

			var (startUtc, endUtc) = ToUtcSlot(date, start, end);

			result.Add(new ScheduleSlot
			{
				SuburbId = suburbId,
				Stage = stage,
				StartTime = TimeOnly.FromDateTime(startUtc),
				EndTime = TimeOnly.FromDateTime(endUtc),
				ScheduleDay = date.DayOfWeek,
				DataHash = $"{suburbId}_{stage}_{startUtc:O}_{endUtc:O}",
				CreatedAt = DateTime.UtcNow
			});
		}

		return result;
	}

	// Deduplicate identical slots (removes feeder duplicates)
	private static List<ScheduleSlot> DeduplicateSlots(List<ScheduleSlot> slots)
	{
		return slots
			.GroupBy(s => s.DataHash)
			.Select(g => g.First())
			.OrderBy(s => s.StartTime)
			.ToList();
	}

	// Merge overlapping / touching slots
	private static List<ScheduleSlot> MergeOverlappingSlots(List<ScheduleSlot> slots)
	{
		if (slots.Count <= 1)
			return slots;

		var ordered = slots.OrderBy(s => s.StartTime).ToList();
		var merged = new List<ScheduleSlot>();

		var current = ordered[0];

		for (int i = 1; i < ordered.Count; i++)
		{
			var next = ordered[i];

			if (current.SuburbId == next.SuburbId &&
				current.Stage == next.Stage &&
				next.StartTime <= current.EndTime)
			{
				current.EndTime = current.EndTime > next.EndTime
					? current.EndTime
					: next.EndTime;
			}
			else
			{
				merged.Add(current);
				current = next;
			}
		}

		merged.Add(current);

		return merged;
	}

	private static bool TryParseTimeRange(
		string input,
		out TimeOnly start,
		out TimeOnly end)
	{
		start = end = default;

		var parts = input.Split('-', StringSplitOptions.TrimEntries);
		if (parts.Length != 2) return false;

		return TimeOnly.TryParse(parts[0], out start)
			&& TimeOnly.TryParse(parts[1], out end);
	}

	private static bool TryParseDate(string input, out DateOnly date)
	{
		date = default;

		// "Wed, 15 Apr"
		var parts = input.Split(',', StringSplitOptions.TrimEntries);
		if (parts.Length != 2) return false;

		var withYear = $"{parts[1]} {DateTime.Now.Year}";
		return DateOnly.TryParse(withYear, out date);
	}

	private static bool IsActiveCell(string content, string cssClass)
	{
		var c = content.ToLower();
		var cls = cssClass.ToLower();

		return c is "x" or "1" or "yes" or "on" or "*"
			|| cls.Contains("active")
			|| cls.Contains("stage")
			|| cls.Contains("on")
			|| (!string.IsNullOrWhiteSpace(content) && content != "-" && content != "&nbsp;");
	}

	public static (DateTime startUtc, DateTime endUtc) ToUtcSlot(
		DateOnly date,
		TimeOnly start,
		TimeOnly end)
	{
		var startLocal = date.ToDateTime(start);

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