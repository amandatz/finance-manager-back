using FinanceManager.Application.Abstractions;
using FinanceManager.Domain.Enums;

namespace FinanceManager.UnitTests.Seeding;

internal sealed class FakeIdentityService : IIdentityService
{
    public bool UsersExist { get; set; }

    public bool CreateUserCalled { get; private set; }

    public Guid? CreatedForHouseholdId { get; private set; }

    public Task<bool> AnyUsersExistAsync(CancellationToken cancellationToken) =>
        Task.FromResult(UsersExist);

    public Task<CreateUserResult> CreateUserAsync(
        string name,
        string email,
        string password,
        Guid householdId,
        HouseholdRole role,
        CancellationToken cancellationToken)
    {
        CreateUserCalled = true;
        CreatedForHouseholdId = householdId;

        return Task.FromResult(new CreateUserResult(CreateUserOutcome.Success, Guid.NewGuid(), []));
    }

    public Task<AuthenticatedUser?> ValidateCredentialsAsync(
        string email, string password, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<UserProfile?> GetUserAsync(Guid userId, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<ChangePasswordOutcome> ChangePasswordAsync(
        Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken) =>
        throw new NotSupportedException();
}
