using FinanceManager.Domain.Enums;

namespace FinanceManager.Application.Transactions.Requests;

public sealed record UpdateTransactionRequest(
    Guid CategoryId,
    TransactionType Type,
    decimal Amount,
    string Currency,
    DateOnly Date,
    string? Description,
    bool? IsHouseholdExpense);
