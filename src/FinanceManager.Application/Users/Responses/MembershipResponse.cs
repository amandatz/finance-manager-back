using FinanceManager.Domain.Enums;

namespace FinanceManager.Application.Users.Responses;

public sealed record MembershipResponse(Guid HouseholdId, string HouseholdName, HouseholdRole Role);
