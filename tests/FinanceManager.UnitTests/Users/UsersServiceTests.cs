using FinanceManager.Application.Abstractions;
using FinanceManager.Application.Users;
using FinanceManager.Application.Users.Requests;
using FinanceManager.Domain.Enums;
using FinanceManager.Domain.Exceptions;

namespace FinanceManager.UnitTests.Users;

public class UsersServiceTests
{
    [Fact]
    public async Task GetCurrentUserAsync_WhenProfileIsNull_ThrowsAuthenticationFailedException()
    {
        var identityService = new ConfigurableFakeIdentityService { UserProfile = null };
        var service = new UsersService(identityService);

        await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => service.GetCurrentUserAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task GetCurrentUserAsync_WhenProfileExists_ReturnsMappedResponse()
    {
        var profile = new UserProfile(Guid.NewGuid(), "Name", "name@test.local", HouseholdRole.Owner, Guid.NewGuid());
        var identityService = new ConfigurableFakeIdentityService { UserProfile = profile };
        var service = new UsersService(identityService);

        var response = await service.GetCurrentUserAsync(profile.Id, profile.HouseholdId, CancellationToken.None);

        Assert.Equal(profile.Id, response.Id);
        Assert.Equal(profile.Email, response.Email);
    }

    [Fact]
    public async Task CreateMemberAsync_WhenEmailAlreadyInUse_ThrowsConflictException()
    {
        var identityService = new ConfigurableFakeIdentityService
        {
            CreateUserResult = new CreateUserResult(CreateUserOutcome.EmailAlreadyInUse, null, []),
        };
        var service = new UsersService(identityService);
        var request = new CreateMemberRequest("Name", "dup@test.local", "SomePassword123");

        await Assert.ThrowsAsync<ConflictException>(
            () => service.CreateMemberAsync(request, Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task CreateMemberAsync_WhenPasswordPolicyViolated_ThrowsBusinessRuleViolationExceptionWithErrors()
    {
        var identityService = new ConfigurableFakeIdentityService
        {
            CreateUserResult = new CreateUserResult(
                CreateUserOutcome.PasswordPolicyViolation,
                null,
                ["Too short.", "Needs a digit."]),
        };
        var service = new UsersService(identityService);
        var request = new CreateMemberRequest("Name", "name@test.local", "short");

        var exception = await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => service.CreateMemberAsync(request, Guid.NewGuid(), CancellationToken.None));

        Assert.Equal(2, exception.Errors.Count);
        Assert.Contains("Too short.", exception.Errors);
    }

    [Fact]
    public async Task CreateMemberAsync_WhenSuccessful_ReturnsCreatedUserResponse()
    {
        var userId = Guid.NewGuid();
        var identityService = new ConfigurableFakeIdentityService
        {
            CreateUserResult = new CreateUserResult(CreateUserOutcome.Success, userId, []),
        };
        var service = new UsersService(identityService);
        var request = new CreateMemberRequest("Name", "name@test.local", "SomePassword123");

        var response = await service.CreateMemberAsync(request, Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(userId, response.Id);
        Assert.Equal(request.Name, response.Name);
        Assert.Equal(request.Email, response.Email);
    }

    [Fact]
    public async Task ChangePasswordAsync_WhenCurrentPasswordIncorrect_ThrowsBusinessRuleViolationException()
    {
        var identityService = new ConfigurableFakeIdentityService
        {
            ChangePasswordOutcome = ChangePasswordOutcome.CurrentPasswordIncorrect,
        };
        var service = new UsersService(identityService);
        var request = new ChangePasswordRequest("WrongPassword123", "NewPassword456");

        await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => service.ChangePasswordAsync(Guid.NewGuid(), request, CancellationToken.None));
    }

    [Fact]
    public async Task ChangePasswordAsync_WhenNewPasswordPolicyViolated_ThrowsBusinessRuleViolationException()
    {
        var identityService = new ConfigurableFakeIdentityService
        {
            ChangePasswordOutcome = ChangePasswordOutcome.NewPasswordPolicyViolation,
        };
        var service = new UsersService(identityService);
        var request = new ChangePasswordRequest("CurrentPassword123", "short");

        await Assert.ThrowsAsync<BusinessRuleViolationException>(
            () => service.ChangePasswordAsync(Guid.NewGuid(), request, CancellationToken.None));
    }

    [Fact]
    public async Task ChangePasswordAsync_WhenSuccessful_CompletesWithoutThrowing()
    {
        var identityService = new ConfigurableFakeIdentityService
        {
            ChangePasswordOutcome = ChangePasswordOutcome.Success,
        };
        var service = new UsersService(identityService);
        var request = new ChangePasswordRequest("CurrentPassword123", "NewPassword456");

        await service.ChangePasswordAsync(Guid.NewGuid(), request, CancellationToken.None);
    }
}
