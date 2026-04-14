namespace GridMonitor.Domain.Entities;

public class Province
{
	public int Id { get; set; }
	public int EskomId { get; set; }
	public string Name { get; set; }
	public DateTime LastSyncedAt { get; set; }
	public IList<Municipality> Municipalities { get; set; }

	public override string ToString()
	{
		return $"{Name} ({EskomId})";
	}
}