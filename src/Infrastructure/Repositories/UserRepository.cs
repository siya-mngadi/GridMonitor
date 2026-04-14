using GridMonitor.Domain.Entities;
using GridMonitor.Domain.Repositories;
using GridMonitor.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;

namespace GridMonitor.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
	private readonly AppDbContext context;

	public UserRepository(AppDbContext context)
	{
		this.context = context;
	}

	public IUnitOfWork UnitOfWork => context;

	public async ValueTask<User> GetByEmailAsync(string email, CancellationToken ct = default)
	{
		return await context.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
	}

	public async ValueTask<User> GetByIdAsync(Guid id, CancellationToken ct = default)
	{
		return await context.Users.FindAsync([id], ct);
	}

	public async ValueTask<User> GetWithSubscriptionsAsync(Guid id, CancellationToken ct = default)
	{
		return await context.Users
			  .Include(u => u.Subscriptions)
				  .ThenInclude(s => s.Channels)
			  .Include(u => u.ApiKeys)
			  .FirstOrDefaultAsync(u => u.Id == id, ct);
	}

	public async ValueTask AddAsync(User user, CancellationToken ct = default)
	{
		await context.Users.AddAsync(user, ct);
	}
}
