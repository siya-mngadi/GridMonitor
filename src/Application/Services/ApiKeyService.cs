using GridMonitor.Application.Helpers;
using GridMonitor.Domain.Entities;
using GridMonitor.Domain.Repositories;
using GridMonitor.Domain.Services;
using GridMonitor.Domain.Shared;
using GridMonitor.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace GridMonitor.Application.Services;

public class ApiKeyService : IApiKeyService
{
	private readonly IApiKeyRepository apiKeyRepository;
	private readonly IUserRepository userRepository;
	private readonly ILogger<ApiKeyService> logger;

	public ApiKeyService(
		IApiKeyRepository apiKeyRepository,
		IUserRepository userRepository,
		ILogger<ApiKeyService> logger)
	{
		this.apiKeyRepository = apiKeyRepository;
		this.userRepository = userRepository;
		this.logger = logger;
	}

	public async ValueTask<Result<ApiKeyResult>> IssueAsync(Guid userId, CancellationToken ct = default)
	{
		var user = await userRepository.GetByIdAsync(userId, ct);
		if (user is null) return Result<ApiKeyResult>.Fail("User not found.");
		if (!user.Active) return Result<ApiKeyResult>.Fail("Account is deactivated.");

		var (plain, hash, prefix) = ApiKeyHelper.Generate();

		var key = new ApiKey
		{
			UserId = userId,
			KeyHash = hash,
			KeyPrefix = prefix,
			Active = true,
			DailyCallLimit = TierPolicy.DailyCallLimit(user.Tier),
			CreatedAt = DateTime.UtcNow
		};

		await apiKeyRepository.AddAsync(key, ct);
		await apiKeyRepository.UnitOfWork.SaveEntitiesAsync(ct);

		logger.LogInformation("API key issued for user {UserId}: {Prefix}", userId, prefix);

		// Plain key returned only here — never stored, never retrievable again
		return Result<ApiKeyResult>.Ok(new ApiKeyResult(prefix, plain, key.Id));
	}

	public async ValueTask<Result> RevokeAsync(Guid keyId, Guid requestingUserId, CancellationToken ct = default)
	{
		var key = await apiKeyRepository.GetByIdAsync(keyId, ct);
		if (key is null) return Result.Fail("Key not found.");
		if (key.UserId != requestingUserId) return Result.Fail("Access denied.");
		if (!key.Active) return Result.Fail("Key is already inactive.");

		await apiKeyRepository.DeactivateAsync(keyId, ct);
		await apiKeyRepository.UnitOfWork.SaveEntitiesAsync(ct);

		logger.LogInformation("API key {KeyId} revoked by user {UserId}", keyId, requestingUserId);
		return Result.Ok();
	}

	public async ValueTask<Result> RotateAsync(Guid keyId, Guid requestingUserId, CancellationToken ct = default)
	{
		// Revoke old, issue new
		var revoke = await RevokeAsync(keyId, requestingUserId, ct);
		if (!revoke.Success) return revoke;

		var issue = await IssueAsync(requestingUserId, ct);
		return issue.Success
			? Result.Ok()
			: Result.Fail(issue.Error!);
	}

	public async ValueTask<Result<ApiKey>> ValidateAsync(string rawKey, CancellationToken ct = default)
	{
		if (string.IsNullOrWhiteSpace(rawKey)) return Result<ApiKey>.Fail("Invalid API key.");
		var hash = ApiKeyHelper.Hash(rawKey);
		var key = await apiKeyRepository.GetByHashAsync(hash, ct);
		if (key is null) return Result<ApiKey>.Fail("API key not found.");
		return Result<ApiKey>.Ok(key);
	}
}
