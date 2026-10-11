using FinanceManager.Domain.Enums;

namespace FinanceManager.Application.FinancialAccounts.Requests;

public sealed record UpdateFinancialAccountRequest(
    string Name,
    FinancialAccountType Type,
    string? InstitutionName);