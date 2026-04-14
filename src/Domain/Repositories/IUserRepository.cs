using GridMonitor.Domain.Entities;

namespace GridMonitor.Domain.Repositories;

public interface IUserRepository : IRepository
{
	ValueTask<User> GetByIdAsync(Guid id, CancellationToken ct = default);
	ValueTask<User> GetByEmailAsync(string email, CancellationToken ct = default);
	ValueTask<User> GetWithSubscriptionsAsync(Guid id, CancellationToken ct = default);
	ValueTask AddAsync(User user, CancellationToken ct = default);
}
