using FinanceManager.Domain.Enums;

namespace FinanceManager.Application.Transactions.Responses;

public sealed record TransactionResponse(
    Guid Id,
    Guid FinancialAccountId,
    Guid CategoryId,
    Guid RecordedByUserId,
    TransactionType Type,
    decimal Amount,
    string Currency,
    DateOnly Date,
    string? Description,
    bool? IsHouseholdExpense,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
