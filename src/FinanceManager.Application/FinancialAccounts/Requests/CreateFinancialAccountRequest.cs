using FinanceManager.Domain.Enums;

namespace FinanceManager.Application.FinancialAccounts.Requests;

public sealed record CreateFinancialAccountRequest(
    string Name,
    FinancialAccountType Type,
    string? InstitutionName);
