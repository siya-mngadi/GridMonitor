using GridMonitor.Domain.Entities;
using GridMonitor.Domain.Repositories;
using GridMonitor.Infrastructure.DataContext;
using Microsoft.EntityFrameworkCore;

namespace GridMonitor.Infrastructure.Repositories;

public class ApiKeyRepository : IApiKeyRepository
{
	private readonly AppDbContext context;

	public ApiKeyRepository(AppDbContext context)
	{
		this.context = context;
	}

	public IUnitOfWork UnitOfWork => context;

	public async ValueTask AddAsync(ApiKey key, CancellationToken ct = default)
	{
		await context.ApiKeys.AddAsync(key, ct);
	}

	public async ValueTask DeactivateAsync(Guid id, CancellationToken ct = default)
	{
		var key = await GetByIdAsync(id, ct);
		key?.Active = false;
	}

	public async ValueTask<ApiKey> GetByHashAsync(string keyHash, CancellationToken ct = default)
	{
		return await context.ApiKeys.FirstOrDefaultAsync(k => k.KeyHash == keyHash && k.Active, ct);
	}

	public async ValueTask<ApiKey> GetByIdAsync(Guid id, CancellationToken ct = default)
	{
		return await context.ApiKeys.FindAsync([id], ct);
	}

	public async ValueTask<List<ApiKey>> GetApiKeysAsync(Guid userId, CancellationToken ct = default)
	{
		return await context.ApiKeys.Where(k => k.Active && k.UserId == userId).ToListAsync(ct);
	}
}
