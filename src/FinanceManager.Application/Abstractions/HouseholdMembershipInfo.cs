using FinanceManager.Domain.Enums;

namespace FinanceManager.Application.Abstractions;

public sealed record HouseholdMembershipInfo(Guid HouseholdId, string HouseholdName, HouseholdRole Role, DateTime CreatedAt);
