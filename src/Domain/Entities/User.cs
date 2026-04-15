using GridMonitor.Domain.Enums;

namespace GridMonitor.Domain.Entities;

public class User
{
	public Guid Id { get; set; }
	public string KeycloakId { get; set; }
	public string Email { get; set; }
	public string Password { get; set; }
	public PricingTier Tier { get; set; }
	public bool Active { get; set; }
	public DateTime CreatedAt { get; set; }
	public IList<AlertSubscription> Subscriptions { get; set; }
	public IList<ApiKey> ApiKeys { get; set; }
}
