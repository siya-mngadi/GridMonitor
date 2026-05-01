using GridMonitor.Domain.Entities;
using GridMonitor.Domain.Enums;
using GridMonitor.Domain.Repositories;
using GridMonitor.Domain.Services;
using GridMonitor.Domain.Shared;
using Keycloak.AuthServices.Sdk;
using Keycloak.AuthServices.Sdk.Admin;
using Keycloak.AuthServices.Sdk.Admin.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GridMonitor.Application.Services;

public class UserService : IUserService
{
	private readonly IUserRepository userRepository;
	private readonly IApiKeyRepository apiKeyRepository;
	private readonly IKeycloakUserClient keycloakClient;
	private readonly ILogger<UserService> logger;

	private readonly KeycloakAdminClientOptions adminClientOptions;

	public UserService(
		IUserRepository usersRepository,
		IApiKeyRepository apiKeysRepository,
		IKeycloakUserClient keycloakClient,
		IOptions<KeycloakAdminClientOptions> adminClientOptions,
		ILogger<UserService> logger)
	{
		this.logger = logger;
		this.userRepository = usersRepository;
		this.apiKeyRepository = apiKeysRepository;
		this.keycloakClient = keycloakClient;
		this.adminClientOptions = adminClientOptions.Value;
	}

	public async ValueTask<Result<User>> GetByIdAsync(Guid id, CancellationToken ct = default)
	{
		var user = await userRepository.GetWithSubscriptionsAsync(id, ct);

		if(user == null) 
			return Result<User>.Fail("User not found.");

		var keycloakUser = await keycloakClient.GetUserAsync(adminClientOptions.Realm, user.KeycloakId, includeUserProfileMetadata:false, ct);

		user.FirstName = keycloakUser?.FirstName;
		user.LastName = keycloakUser?.LastName;
		user.EmailVerified = keycloakUser?.EmailVerified ?? false;

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

		await keycloakClient.CreateUserAsync(adminClientOptions.Realm, new UserRepresentation
		{
			Username = user.Email,
			Email = user.Email,
			Enabled = true,
			Attributes = new Dictionary<string, ICollection<string>>
			{
				{ "pricing_tier", [ user.Tier.ToString() ] }
			}
		}, ct);

		await userRepository.AddAsync(user, ct);

		await userRepository.UnitOfWork.SaveEntitiesAsync(ct);

		logger.LogInformation("User registered: {Email}", user.Email);
		return Result<User>.Ok(user);
	}

	public async ValueTask<Result> DeactivateAsync(Guid userId, CancellationToken ct = default)
	{
		var user = await userRepository.GetByIdAsync(userId, ct);
		if (user is null) return Result.Fail("User not found.");

		// Deactivate user account on Keycloak.
		await keycloakClient.UpdateUserAsync(adminClientOptions.Realm, user.KeycloakId, new UserRepresentation { Enabled = false }, ct);

		// Deactivate user in our system
		user.Active = false;
		// Deactivate all API keys — prevent further API access immediately
		var keys = await apiKeyRepository.GetApiKeysAsync(userId, ct);
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

		var keycloakUser = new UserRepresentation
		{
			Attributes = new Dictionary<string, ICollection<string>>
			{
				{ "pricing_tier", [ newTier.ToString() ] }
			}
		};

		// Update tier on Keycloak.
		await keycloakClient.UpdateUserAsync(adminClientOptions.Realm, user.KeycloakId, keycloakUser, ct);

		// Update daily call limits on all active API keys to reflect new tier
		var keys = await apiKeyRepository.GetApiKeysAsync(userId, ct);
		foreach (var k in keys.Where(k => k.Active))
			k.DailyCallLimit = TierPolicy.DailyCallLimit(newTier);

		// Update on our database
		await userRepository.UnitOfWork.SaveEntitiesAsync(ct);
		logger.LogInformation("User {Id} tier changed {Old} → {New}", userId, oldTier, newTier);
		return Result.Ok();
	}
}
