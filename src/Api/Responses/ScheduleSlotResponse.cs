namespace GridMonitor.Api.Responses;

public record ScheduleSlotResponse(
	int SuburbId,
	short Stage,
	TimeOnly StartTime,
	TimeOnly EndTime,
	DayOfWeek ScheduleDay,
	DateTime CreatedAt
);