namespace GridMonitor.Domain.Entities;

public record SuburbSchedule(string SuburbName, int CurrentStage, IReadOnlyList<ScheduleSlot> UpcomingSlots);
