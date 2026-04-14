namespace GridMonitor.Domain.Entities;

public class Municipality
{
	public int Id { get; set; }
	public int EskomId { get; set; }
	public string Name { get; set; }
	public int ProvinceId { get; set; }
	public int Total { get; set; }
	public DateTime LastSyncedAt { get; set; }
	public Province Province { get; set; }
	public IList<Suburb> Suburbs { get; set; }

	public override string ToString()
	{
		return $"{Name} ({EskomId})";
	}
}
