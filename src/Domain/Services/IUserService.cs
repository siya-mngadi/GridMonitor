using GridMonitor.Domain.Entities;
using GridMonitor.Domain.Enums;
using GridMonitor.Domain.Shared;

namespace GridMonitor.Domain.Services;

public interface IUserService
{
	ValueTask<Result<User>> RegisterAsync(string email, string passwordHash, CancellationToken ct = default);

	ValueTask<Result<User>> GetByIdAsync(Guid id, CancellationToken ct = default);

	ValueTask<Result> UpgradeTierAsync(Guid userId, PricingTier newTier, CancellationToken ct = default);

	ValueTask<Result> DeactivateAsync(Guid userId, CancellationToken ct = default);
}
