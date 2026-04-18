using GridMonitor.Domain.Entities;
using GridMonitor.Domain.Shared;
using GridMonitor.Domain.ValueObjects;

namespace GridMonitor.Domain.Services;

public interface IApiKeyService
{
	ValueTask<Result<ApiKeyResult>> IssueAsync(Guid userId, CancellationToken ct = default);
	ValueTask<Result<List<ApiKey>>> GetApiKeysAsync(Guid userId, CancellationToken ct = default);
	ValueTask<Result<ApiKey>> ValidateAsync(string rawKey, CancellationToken ct = default);
	ValueTask<Result> RevokeAsync(Guid keyId, Guid requestingUserId, CancellationToken ct = default);
	ValueTask<Result<ApiKeyResult>> RotateAsync(Guid keyId, Guid requestingUserId, CancellationToken ct = default);
}
