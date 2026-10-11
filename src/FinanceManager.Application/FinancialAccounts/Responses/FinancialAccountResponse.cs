using FinanceManager.Domain.Enums;

namespace FinanceManager.Application.FinancialAccounts.Responses;

public sealed record FinancialAccountResponse(
    Guid Id,
    string Name,
    FinancialAccountType Type,
    string? InstitutionName,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);