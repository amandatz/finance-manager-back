using FinanceManager.Domain.Enums;

namespace FinanceManager.Application.Abstractions;

public interface ITokenGenerator
{
    string GenerateAccessToken(Guid userId, Guid householdId, HouseholdRole role);
}
