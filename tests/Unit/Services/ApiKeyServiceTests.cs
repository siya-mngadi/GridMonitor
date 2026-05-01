using FluentAssertions;
using GridMonitor.Application.Helpers;
using GridMonitor.Application.Services;
using GridMonitor.Domain.Entities;
using GridMonitor.Domain.Enums;
using GridMonitor.Domain.Repositories;
using GridMonitor.Domain.Shared;
using GridMonitor.Tests.Unit.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace GridMonitor.Tests.Unit.Services;

public class ApiKeyServiceTests
{
	private readonly IApiKeyRepository _keys = Substitute.For<IApiKeyRepository>();
	private readonly IUserRepository _users = Substitute.For<IUserRepository>();
	private readonly ApiKeyService _service;

	public ApiKeyServiceTests()
	{
		_service = new ApiKeyService(_keys, _users, NullLogger<ApiKeyService>.Instance);
	}

	[Fact]
	public async Task Issue_ActiveUser_ReturnsPlainKey_StartsWithLsPrefix()
	{
		var user = GenerateMockObjects.User(tier: PricingTier.Free);
		_users.GetByIdAsync(user.Id).Returns(user);

		var result = await _service.IssueAsync(user.Id);

		result.Success.Should().BeTrue();
		result.Value!.PlainKey.Should().StartWith("ls_");
	}

	[Fact]
	public async Task Issue_ActiveUser_PrefixEndsWithEllipsis()
	{
		var user = GenerateMockObjects.User();
		_users.GetByIdAsync(user.Id).Returns(user);

		var result = await _service.IssueAsync(user.Id);

		result.Value!.Prefix.Should().EndWith("...");
	}

	[Fact]
	public async Task Issue_SetsCorrectDailyLimitForTier()
	{
		var user = GenerateMockObjects.User(tier: PricingTier.Starter);
		_users.GetByIdAsync(user.Id).Returns(user);

		await _service.IssueAsync(user.Id);

		await _keys.Received(1).AddAsync(Arg.Is<ApiKey>(k =>
			k.DailyCallLimit == TierPolicy.DailyCallLimit(PricingTier.Starter)));
	}

	[Fact]
	public async Task Issue_TwoCallsForSameUser_ProduceDifferentKeys()
	{
		var user = GenerateMockObjects.User();
		_users.GetByIdAsync(user.Id).Returns(user);

		var r1 = await _service.IssueAsync(user.Id);
		var r2 = await _service.IssueAsync(user.Id);

		r1.Value!.PlainKey.Should().NotBe(r2.Value!.PlainKey,
			because: "every issued key must be unique");
	}

	[Fact]
	public async Task Issue_UserNotFound_Fails()
	{
		_users.GetByIdAsync(Arg.Any<Guid>()).Returns(default(User));

		var result = await _service.IssueAsync(Guid.NewGuid());

		result.Success.Should().BeFalse();
		result.Error.Should().Contain("not found");
		await _keys.DidNotReceive().AddAsync(Arg.Any<ApiKey>());
	}

	[Fact]
	public async Task Issue_DeactivatedUser_Fails()
	{
		var user = GenerateMockObjects.User(isActive: false);
		_users.GetByIdAsync(user.Id).Returns(user);

		var result = await _service.IssueAsync(user.Id);

		result.Success.Should().BeFalse();
		result.Error.Should().Contain("deactivated");
	}

	[Fact]
	public async Task Validate_ValidKey_ReturnsKeyEntity()
	{
		var (plain, hash, prefix) = ApiKeyHelper.Generate();
		var stored = GenerateMockObjects.ApiKey(Guid.NewGuid());
		stored.KeyHash = hash;
		_keys.GetByHashAsync(hash).Returns(stored);

		var result = await _service.ValidateAsync(plain);

		result.Should().NotBeNull();
		result!.Value.KeyHash.Should().Be(hash);
	}

	[Fact]
	public async Task Validate_UnknownKey_ReturnsNull()
	{
		_keys.GetByHashAsync(Arg.Any<string>()).Returns(default(ApiKey));

		var result = await _service.ValidateAsync("ls_unknown_key_value");

		result.Value.Should().BeNull();
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData(null!)]
	public async Task Validate_EmptyOrNullKey_ReturnsNull_WithoutQueryingDb(string rawKey)
	{
		var result = await _service.ValidateAsync(rawKey);

		result.Value.Should().BeNull();
		await _keys.DidNotReceive().GetByHashAsync(Arg.Any<string>());
	}

	[Fact]
	public async Task Validate_HashIsComputedFromRawKey()
	{
		// Ensure the service hashes before looking up — wrong hash = null
		var (plain, hash, _) = ApiKeyHelper.Generate();
		_keys.GetByHashAsync(hash).Returns(GenerateMockObjects.ApiKey(Guid.NewGuid()));

		// Pass the plain key — service must hash it internally before lookup
		var result = await _service.ValidateAsync(plain);

		result.Should().NotBeNull();
		await _keys.Received(1).GetByHashAsync(hash);
	}

	// ── RevokeAsync ───────────────────────────────────────────────────────────

	[Fact]
	public async Task Revoke_OwnActiveKey_Succeeds()
	{
		var userId = Guid.NewGuid();
		var key = GenerateMockObjects.ApiKey(userId, isActive: true);
		_keys.GetByIdAsync(key.Id).Returns(key);

		var result = await _service.RevokeAsync(key.Id, userId);

		result.Success.Should().BeTrue();
		await _keys.Received(1).DeactivateAsync(key.Id);
		await _keys.Received(1).UnitOfWork.SaveEntitiesAsync();
	}

	[Fact]
	public async Task Revoke_AnotherUsersKey_Fails_DoesNotDeactivate()
	{
		var key = GenerateMockObjects.ApiKey(Guid.NewGuid());
		_keys.GetByIdAsync(key.Id).Returns(key);

		var result = await _service.RevokeAsync(key.Id, Guid.NewGuid()); // different userId

		result.Success.Should().BeFalse();
		result.Error.Should().Contain("denied");
		await _keys.DidNotReceive().DeactivateAsync(Arg.Any<Guid>());
	}

	[Fact]
	public async Task Revoke_AlreadyInactiveKey_Fails()
	{
		var userId = Guid.NewGuid();
		var key = GenerateMockObjects.ApiKey(userId, isActive: false);
		_keys.GetByIdAsync(key.Id).Returns(key);

		var result = await _service.RevokeAsync(key.Id, userId);

		result.Success.Should().BeFalse();
		result.Error.Should().Contain("already inactive");
	}

	[Fact]
	public async Task Revoke_KeyNotFound_Fails()
	{
		_keys.GetByIdAsync(Arg.Any<Guid>()).Returns(default(ApiKey));

		var result = await _service.RevokeAsync(Guid.NewGuid(), Guid.NewGuid());

		result.Success.Should().BeFalse();
		result.Error.Should().Contain("not found");
	}

	[Fact]
	public async Task Rotate_ValidKey_RevokesOldAndIssuesNew()
	{
		var user = GenerateMockObjects.User();
		var key = GenerateMockObjects.ApiKey(user.Id, isActive: true);

		_keys.GetByIdAsync(key.Id).Returns(key);
		_users.GetByIdAsync(user.Id).Returns(user);

		var result = await _service.RotateAsync(key.Id, user.Id);

		result.Success.Should().BeTrue();
		await _keys.Received(1).DeactivateAsync(key.Id);
		await _keys.Received(1).AddAsync(Arg.Any<ApiKey>()); // new key issued
	}

	[Fact]
	public async Task Rotate_KeyNotFound_Fails_DoesNotIssueNew()
	{
		_keys.GetByIdAsync(Arg.Any<Guid>()).Returns(default(ApiKey));

		var result = await _service.RotateAsync(Guid.NewGuid(), Guid.NewGuid());

		result.Success.Should().BeFalse();
		await _users.DidNotReceive().GetByIdAsync(Arg.Any<Guid>());
	}
}
