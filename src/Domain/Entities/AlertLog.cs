using GridMonitor.Domain.Enums;

namespace GridMonitor.Domain.Entities;

public class AlertLog
{
	public long Id { get; set; }
	public Guid SubscriptionId { get; set; }
	public Channel ChannelType { get; set; }
	public string Destination { get; set; }
	public string IdemptotencyKey { get; set; }
	public short Stage { get; set; }
	public AlertEvent Event { get; set; }
	public bool Success { get; set; }
	public string ErrorMessage { get; set; }
	public short AttemptCount { get; set; }
	public DateTime SentAt { get; set; }
	public AlertSubscription Subscription { get; set; }
}
