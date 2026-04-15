using GridMonitor.Application.Helpers;
using GridMonitor.Domain.Entities;
using GridMonitor.Domain.Enums;
using GridMonitor.Domain.Repositories;
using GridMonitor.Domain.Services;
using GridMonitor.Domain.Shared;
using Microsoft.Extensions.Logging;

namespace GridMonitor.Application.Services;

public class UserService : IUserService
{
	private readonly IUserRepository userRepository;
	private readonly IApiKeyRepository apiKeyRepository;
	private readonly ILogger<UserService> logger;

	public UserService(
		IUserRepository usersRepository,
		IApiKeyRepository apiKeysRepository,
		ILogger<UserService> logger)
	{
		this.userRepository = usersRepository;
		this.apiKeyRepository = apiKeysRepository;
		this.logger = logger;
	}

	public async ValueTask<Result<User>> GetByIdAsync(Guid id, CancellationToken ct = default)
	{
		var user = await userRepository.GetWithSubscriptionsAsync(id, ct);
		return user is null
			? Result<User>.Fail("User not found.")
			: Result<User>.Ok(user);
	}

	public async ValueTask<Result<User>> RegisterAsync(string email, string passwordHash, CancellationToken ct = default)
	{
		if (string.IsNullOrWhiteSpace(email))
			return Result<User>.Fail("Email is required.");

		if (string.IsNullOrWhiteSpace(passwordHash))
			return Result<User>.Fail("Password hash is required.");

		var existing = await userRepository.GetByEmailAsync(email.Trim().ToLower(), ct);
		if (existing is not null)
			return Result<User>.Fail("An account with that email already exists.");

		var user = new User
		{
			Email = email.Trim().ToLower(),
			Password = passwordHash,
			Tier = PricingTier.Free,
			Active = true,
			CreatedAt = DateTime.UtcNow
		};

		await userRepository.AddAsync(user, ct);

		// Provision a starter API key automatically on registration
		//var (plain, hash, prefix) = ApiKeyHelper.Generate();
		//var key = new ApiKey
		//{
		//	UserId = user.Id,
		//	KeyHash = hash,
		//	KeyPrefix = prefix,
		//	Active = true,
		//	DailyCallLimit = TierPolicy.DailyCallLimit("free"),
		//	CreatedAt = DateTime.UtcNow
		//};

		//await apiKeyRepository.AddAsync(key, ct);
		await userRepository.UnitOfWork.SaveEntitiesAsync(ct);

		logger.LogInformation("User registered: {Email}", user.Email);
		return Result<User>.Ok(user);
	}

	public async ValueTask<Result> DeactivateAsync(Guid userId, CancellationToken ct = default)
	{
		var user = await userRepository.GetByIdAsync(userId, ct);
		if (user is null) return Result.Fail("User not found.");

		user.Active = false;

		// Deactivate all API keys — prevent further API access immediately
		var keys = await apiKeyRepository.GetByUserAsync(userId, ct);
		foreach (var k in keys) k.Active = false;

		await userRepository.UnitOfWork.SaveEntitiesAsync(ct);
		logger.LogWarning("User {Id} deactivated", userId);
		return Result.Ok();
	}

	public async ValueTask<Result> UpgradeTierAsync(Guid userId, PricingTier newTier, CancellationToken ct = default)
	{
		var user = await userRepository.GetByIdAsync(userId, ct);
		if (user is null) return Result.Fail("User not found.");
		if (!user.Active) return Result.Fail("Account is deactivated.");

		var oldTier = user.Tier;
		user.Tier = newTier;

		// Update daily call limits on all active API keys to reflect new tier
		var keys = await apiKeyRepository.GetByUserAsync(userId, ct);
		foreach (var k in keys.Where(k => k.Active))
			k.DailyCallLimit = TierPolicy.DailyCallLimit(newTier);

		await userRepository.UnitOfWork.SaveEntitiesAsync(ct);
		logger.LogInformation("User {Id} tier changed {Old} → {New}", userId, oldTier, newTier);
		return Result.Ok();
	}
}
