namespace GridMonitor.Domain.Entities;

public class Suburb
{
	public int Id { get; set; }
	public int EskomId { get; set; }
	public string Name { get; set; }
	public char Block => Name.Split('_').LastOrDefault()?.FirstOrDefault() ?? 'A';  // Assume name contains block info, e.g., "Suburb_A" -> Block = 'A'
	public int MunicipalityId { get; set; }
	public int Total { get; set; }
	public DateTime LastSyncedAt { get; set; }
	public Municipality Municipality { get; set; }
	public IList<ScheduleSlot> Slots { get; set; }

	public override string ToString()
	{
		return $"{Name} ({EskomId})";
	}
}
