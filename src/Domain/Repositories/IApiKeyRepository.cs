using GridMonitor.Domain.Entities;

namespace GridMonitor.Domain.Repositories;

public interface IApiKeyRepository : IRepository
{
	ValueTask<ApiKey> GetByHashAsync(string keyHash, CancellationToken ct = default);
	ValueTask<ApiKey> GetByIdAsync(Guid id, CancellationToken ct = default);
	ValueTask<List<ApiKey>> GetByUserAsync(Guid userId, CancellationToken ct = default);
	ValueTask AddAsync(ApiKey key, CancellationToken ct = default);
	ValueTask DeactivateAsync(Guid id, CancellationToken ct = default);
}
