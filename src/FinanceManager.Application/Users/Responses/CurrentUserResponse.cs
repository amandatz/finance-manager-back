using FinanceManager.Domain.Enums;

namespace FinanceManager.Application.Users.Responses;

public sealed record CurrentUserResponse(
    Guid Id,
    string Name,
    string Email,
    HouseholdRole Role,
    Guid HouseholdId);
