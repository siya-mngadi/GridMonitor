namespace GridMonitor.Domain.Entities;

public class StageSnapshot
{
	public int Id { get; set; }
	public short Stage { get; set; }
	public string RawText { get; set; }
	public DateTime CreatedAt { get; set; }

	public override string ToString()
	{
		return $"Stage: {Stage}";
	}
}
