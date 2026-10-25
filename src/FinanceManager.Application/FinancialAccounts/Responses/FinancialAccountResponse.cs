using FinanceManager.Domain.Enums;

namespace FinanceManager.Application.FinancialAccounts.Responses;

public sealed record FinancialAccountResponse(
    Guid Id,
    string Name,
    FinancialAccountType Type,
    string? InstitutionName,
    IReadOnlyCollection<Guid> OwnerUserIds,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
