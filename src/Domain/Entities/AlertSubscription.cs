namespace GridMonitor.Domain.Entities;

public class AlertSubscription
{
	public Guid Id { get; set; }
	public Guid UserId { get; set; }
	public int SuburbId { get; set; }
	public short AlertMinutesBefore { get; set; }
	public bool Active { get; set; }
	public DateTime CreatedAt { get; set; }
	public User User { get; set; }
	public Suburb Suburb { get; set; }
	public IList<AlertChannel> Channels { get; set; }
	public IList<AlertSubscription> Subscriptions { get; set; }
	public IList<AlertLog> Logs { get; set; }
}
