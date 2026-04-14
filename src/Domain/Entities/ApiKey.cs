namespace GridMonitor.Domain.Entities;

public class ApiKey
{
	public Guid Id { get; set; }
	public Guid UserId { get; set; }
	public string KeyHash { get; set; }
	public string KeyPrefix { get; set; }
	public bool Active { get; set; }
	public int DailyCallLimit { get; set; } = 50;
	public DateTime CreatedAt { get; set; }
	public DateTime? DeletedAt { get; set; }
	public User User { get; set; }
}
