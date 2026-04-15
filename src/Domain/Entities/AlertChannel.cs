using GridMonitor.Domain.Enums;

namespace GridMonitor.Domain.Entities;

public class AlertChannel
{
	public Guid Id { get; set; }
	public Guid SubscriptionId { get; set; }
	public ChannelType ChannelType { get; set; }
	public string Destination { get; set; }
	public string WebhookSecret { get; set; }
	public bool Active { get; set; }
	public AlertSubscription Subscription { get; set; }
}
