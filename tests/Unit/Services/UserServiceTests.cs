using FluentAssertions;
using GridMonitor.Application.Services;
using GridMonitor.Domain.Entities;
using GridMonitor.Domain.Enums;
using GridMonitor.Domain.Repositories;
using GridMonitor.Domain.Shared;
using GridMonitor.Tests.Unit.Shared;
using Keycloak.AuthServices.Sdk;
using Keycloak.AuthServices.Sdk.Admin;
using Keycloak.AuthServices.Sdk.Admin.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace GridMonitor.Tests.Unit.Services;

public class UserServiceTests
{
	private readonly IUserRepository _users = Substitute.For<IUserRepository>();
	private readonly IApiKeyRepository _apiKeys = Substitute.For<IApiKeyRepository>();
	private readonly IKeycloakUserClient _keycloak = Substitute.For<IKeycloakUserClient>();
	private readonly UserService _userService;

	public UserServiceTests()
	{
		var options = new KeycloakAdminClientOptions
		{
			AuthServerUrl = "https://keycloak.local",
			Realm = "my-realm",
		};

		var optionsMock = Substitute.For<IOptions<KeycloakAdminClientOptions>>();
		optionsMock.Value.Returns(options);
		_userService = new UserService(_users, _apiKeys, _keycloak, optionsMock, NullLogger<UserService>.Instance);
	}

	[Fact]
	public async Task Register_NewEmail_Succeeds_SetsFreeTier()
	{
		_users.GetByEmailAsync(Arg.Any<string>()).Returns(default(User));

		var result = await _userService.RegisterAsync("Alice@Test.com", "hash");

		result.Success.Should().BeTrue();
		result.Value!.Email.Should().Be("alice@test.com", because: "email is normalised to lowercase");
		result.Value.Tier.Should().Be(PricingTier.Free);
		result.Value.Active.Should().BeTrue();
	}

	[Fact]
	public async Task Register_NewEmail_AddsUserAndApiKey_AndSaves()
	{
		_users.GetByEmailAsync(Arg.Any<string>()).Returns(default(User));

		await _userService.RegisterAsync("new@test.com", "hash");

		await _users.Received(1).AddAsync(Arg.Is<User>(u => u.Email == "new@test.com"));
		await _users.Received(1).UnitOfWork.SaveEntitiesAsync();
	}

	[Fact]
	public async Task Register_DuplicateEmail_Fails_DoesNotAddUser()
	{
		_users.GetByEmailAsync(Arg.Any<string>()).Returns(GenerateMockObjects.User());

		var result = await _userService.RegisterAsync("existing@test.com", "hash");

		result.Success.Should().BeFalse();
		result.Error.Should().Contain("already exists");
		await _users.DidNotReceive().AddAsync(Arg.Any<User>());
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	public async Task Register_EmptyEmail_Fails(string email)
	{
		var result = await _userService.RegisterAsync(email, "hash");

		result.Success.Should().BeFalse();
		result.Error.Should().Contain("required");
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	public async Task Register_EmptyPasswordHash_Fails(string hash)
	{
		var result = await _userService.RegisterAsync("test@test.com", hash);

		result.Success.Should().BeFalse();
		result.Error.Should().Contain("required");
	}

	[Fact]
	public async Task GetById_ExistingUser_ReturnsUser()
	{
		var user = GenerateMockObjects.User();
		_keycloak.GetUserAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(new UserRepresentation
		{
			Email = user.Email,
			Enabled = user.Active,
			Attributes = new Dictionary<string, ICollection<string>>
			{
				["pricing_tier"] = [user.Tier.ToString()]
			}
		});
		_users.GetWithSubscriptionsAsync(user.Id).Returns(user);


		var result = await _userService.GetByIdAsync(user.Id);

		result.Success.Should().BeTrue();
		result.Value.Should().Be(user);
	}

	[Fact]
	public async Task GetById_MissingUser_Fails()
	{
		_keycloak.GetUserAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(default(UserRepresentation));
		_users.GetWithSubscriptionsAsync(Arg.Any<Guid>()).Returns(default(User));

		var result = await _userService.GetByIdAsync(Guid.NewGuid());

		result.Success.Should().BeFalse();
		result.Error.Should().Contain("not found");
	}

	// UpgradeTierAsync
	[Theory]
	[InlineData(PricingTier.Free, PricingTier.Starter)]
	[InlineData(PricingTier.Starter, PricingTier.Pro)]
	[InlineData(PricingTier.Pro, PricingTier.Free)]
	public async Task UpgradeTier_ValidTransition_UpdatesUserTier(PricingTier from, PricingTier to)
	{
		var user = GenerateMockObjects.User(tier: from);
		_users.GetByIdAsync(user.Id).Returns(user);
		_apiKeys.GetApiKeysAsync(user.Id).Returns([]);

		var result = await _userService.UpgradeTierAsync(user.Id, to);

		result.Success.Should().BeTrue();
		user.Tier.Should().Be(to);
		await _users.Received(1).UnitOfWork.SaveEntitiesAsync();
	}

	[Fact]
	public async Task UpgradeTier_UpdatesAllActiveApiKeyLimits()
	{
		var user = GenerateMockObjects.User(tier: PricingTier.Free);
		var active = GenerateMockObjects.ApiKey(user.Id, PricingTier.Free, isActive: true);
		var inactive = GenerateMockObjects.ApiKey(user.Id, PricingTier.Free, isActive: false);

		_users.GetByIdAsync(user.Id).Returns(user);
		_apiKeys.GetApiKeysAsync(user.Id).Returns(new List<ApiKey> { active, inactive });

		await _userService.UpgradeTierAsync(user.Id, PricingTier.Pro);

		active.DailyCallLimit.Should().Be(int.MaxValue,
			because: "active key gets the new tier limit");
		inactive.DailyCallLimit.Should().Be(TierPolicy.DailyCallLimit(PricingTier.Free),
			because: "inactive keys are not updated");
	}

	[Fact]
	public async Task UpgradeTier_UserNotFound_Fails()
	{
		_users.GetByIdAsync(Arg.Any<Guid>()).Returns(default(User));

		var result = await _userService.UpgradeTierAsync(Guid.NewGuid(), PricingTier.Starter);

		result.Success.Should().BeFalse();
		result.Error.Should().Contain("not found");
	}

	[Fact]
	public async Task UpgradeTier_DeactivatedUser_Fails()
	{
		var user = GenerateMockObjects.User(isActive: false);
		_users.GetByIdAsync(user.Id).Returns(user);

		var result = await _userService.UpgradeTierAsync(user.Id, PricingTier.Starter);

		result.Success.Should().BeFalse();
		result.Error.Should().Contain("deactivated");
	}

	// DeactivateAsync

	[Fact]
	public async Task Deactivate_ActiveUser_SetsUserInactive()
	{
		var user = GenerateMockObjects.User();
		_keycloak.UpdateUserAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<UserRepresentation>()).Returns(Task.CompletedTask);
		_users.GetByIdAsync(user.Id).Returns(user);
		_apiKeys.GetApiKeysAsync(user.Id).Returns([]);

		await _userService.DeactivateAsync(user.Id);

		user.Active.Should().BeFalse();
	}

	[Fact]
	public async Task Deactivate_DeactivatesAllApiKeys_RegardlessOfActiveStatus()
	{
		var user = GenerateMockObjects.User();
		var k1 = GenerateMockObjects.ApiKey(user.Id, isActive: true);
		var k2 = GenerateMockObjects.ApiKey(user.Id, isActive: true);
		var k3 = GenerateMockObjects.ApiKey(user.Id, isActive: false);

		_keycloak.UpdateUserAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<UserRepresentation>()).Returns(Task.CompletedTask);
		_users.GetByIdAsync(user.Id).Returns(user);
		_apiKeys.GetApiKeysAsync(user.Id).Returns([k1, k2, k3]);

		await _userService.DeactivateAsync(user.Id);

		k1.Active.Should().BeFalse();
		k2.Active.Should().BeFalse();
		k3.Active.Should().BeFalse();
		await _users.Received(1).UnitOfWork.SaveEntitiesAsync();
	}

	[Fact]
	public async Task Deactivate_UserNotFound_Fails()
	{
		_keycloak.UpdateUserAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<UserRepresentation>()).Returns(Task.CompletedTask);
		_users.GetByIdAsync(Arg.Any<Guid>()).Returns(default(User));

		var result = await _userService.DeactivateAsync(Guid.NewGuid());

		result.Success.Should().BeFalse();
		result.Error.Should().Contain("not found");
	}
}
