using FinanceManager.Application.Abstractions;
using FinanceManager.Application.Households.Responses;
using FinanceManager.Application.Users.Responses;
using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;
using FinanceManager.Domain.Exceptions;
using FinanceManager.Domain.Repositories;

namespace FinanceManager.Application.Households;

public sealed class HouseholdsService(
    IHouseholdRepository householdRepository,
    IIdentityService identityService,
    IRefreshTokenService refreshTokenService)
{
    public async Task<InviteCodeResponse> GenerateInviteCodeAsync(
        Guid householdId,
        CancellationToken cancellationToken)
    {
        var household = await householdRepository.GetByIdAsync(householdId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Household), householdId);

        var code = household.GenerateInviteCode();
        await householdRepository.SaveChangesAsync(cancellationToken);

        return new InviteCodeResponse(code);
    }

    public async Task<LoginResponse> JoinAsync(Guid userId, string inviteCode, CancellationToken cancellationToken)
    {
        var household = await householdRepository.GetByInviteCodeAsync(inviteCode, cancellationToken)
            ?? throw new BusinessRuleViolationException("The invite code is invalid or has already been used.");

        var result = await identityService.AddMembershipAsync(
            userId, household.Id, HouseholdRole.Member, cancellationToken);

        if (result.Outcome == AddMembershipOutcome.AlreadyMember)
        {
            throw new ConflictException("Already a member of this household.");
        }

        household.ConsumeInviteCode();
        await householdRepository.SaveChangesAsync(cancellationToken);

        var tokens = await refreshTokenService.IssueTokensAsync(
            userId, household.Id, HouseholdRole.Member, cancellationToken);

        return new LoginResponse(tokens.AccessToken, tokens.RefreshToken);
    }
}
