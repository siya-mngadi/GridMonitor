namespace GridMonitor.Domain.Entities;

public class ScheduleSlot
{
	public long Id { get; set; }
	public int SuburbId { get; set; }
	public short Stage { get; set; }
	public TimeOnly StartTime { get; set; }
	public TimeOnly EndTime { get; set; }
	public string DayLabel => ScheduleDay.ToString();
	public DayOfWeek ScheduleDay { get; set; }
	public string DataHash { get; set; }
	public DateTime CreatedAt { get; set; }
	public Suburb Suburb { get; set; }
}
