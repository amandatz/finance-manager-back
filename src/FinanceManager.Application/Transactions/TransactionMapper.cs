using FinanceManager.Application.Transactions.Responses;
using FinanceManager.Domain.Entities;

namespace FinanceManager.Application.Transactions;

public static class TransactionMapper
{
    public static TransactionResponse Map(Transaction transaction) =>
        new(
            transaction.Id,
            transaction.FinancialAccountId,
            transaction.CreditCardId,
            transaction.CategoryId,
            transaction.RecordedByUserId,
            transaction.Type,
            transaction.Amount,
            transaction.Currency,
            transaction.Date,
            transaction.Description,
            transaction.IsHouseholdExpense,
            transaction.SettlementGroupId,
            transaction.IsActive,
            transaction.CreatedAt,
            transaction.UpdatedAt);
}
