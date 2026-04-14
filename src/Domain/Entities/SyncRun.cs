namespace GridMonitor.Domain.Entities;

public class SyncRun
{
	public int Id { get; set; }
	public string Type { get; set; }
	public bool Success { get; set; }
	public int MunicipalitiesProcessed { get; set; }
	public int SuburbProcessed { get; set; }
	public string ErrorMessage { get; set; }
	public DateTime StartedAt { get; set; } = DateTime.UtcNow;
	public DateTime? FinishedAt { get; set; }

	public override string ToString()
	{
		return $"{Type}: Municipalities = {MunicipalitiesProcessed}, Suburbs = {SuburbProcessed}  (Success: {Success})";
	}
}
