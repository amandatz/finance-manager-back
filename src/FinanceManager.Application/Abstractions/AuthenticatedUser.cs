using FinanceManager.Domain.Enums;

namespace FinanceManager.Application.Abstractions;

public sealed record AuthenticatedUser(Guid UserId, Guid HouseholdId, HouseholdRole Role);
