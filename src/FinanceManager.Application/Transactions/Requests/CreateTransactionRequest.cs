using FinanceManager.Domain.Enums;

namespace FinanceManager.Application.Transactions.Requests;

public sealed record CreateTransactionRequest(
    Guid FinancialAccountId,
    Guid CategoryId,
    TransactionType Type,
    decimal Amount,
    string Currency,
    DateOnly Date,
    string? Description,
    bool? IsHouseholdExpense);
