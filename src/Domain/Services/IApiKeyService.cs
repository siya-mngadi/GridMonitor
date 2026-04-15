using GridMonitor.Domain.Entities;
using GridMonitor.Domain.Shared;
using GridMonitor.Domain.ValueObjects;

namespace GridMonitor.Domain.Services;

public interface IApiKeyService
{
	// Returns the plain key exactly once — caller must show it to the user immediately
	ValueTask<Result<ApiKeyResult>> IssueAsync(Guid userId, CancellationToken ct = default);
	// Validates a raw key from a request header — returns the key entity if valid
	ValueTask<Result<ApiKey>> ValidateAsync(string rawKey, CancellationToken ct = default);
	ValueTask<Result> RevokeAsync(Guid keyId, Guid requestingUserId, CancellationToken ct = default);
	ValueTask<Result> RotateAsync(Guid keyId, Guid requestingUserId, CancellationToken ct = default);
}
