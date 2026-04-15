using GridMonitor.Domain.Entities;
using GridMonitor.Domain.Enums;

namespace GridMonitor.Application.Helpers;

public class ApiKeyContext
{
	public ApiKey Key { get; set; }
	public bool IsValid => Key is not null && Key.Active;
	public PricingTier Tier => Key?.User?.Tier ?? PricingTier.Free;
	public Guid UserId => Key?.UserId ?? Guid.Empty;
	public Guid KeyId => Key?.Id ?? Guid.Empty;
}
