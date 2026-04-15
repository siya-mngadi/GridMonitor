namespace GridMonitor.Api.Responses;

public record SuburbScheduleResponse(string SuburbName, int CurrentStage, IEnumerable<ScheduleSlotResponse> Slots);
