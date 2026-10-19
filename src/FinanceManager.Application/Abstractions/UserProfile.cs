using FinanceManager.Domain.Enums;

namespace FinanceManager.Application.Abstractions;

public sealed record UserProfile(Guid Id, string Name, string Email, HouseholdRole Role, Guid HouseholdId);
