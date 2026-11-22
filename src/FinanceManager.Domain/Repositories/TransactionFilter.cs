using FinanceManager.Domain.Enums;

namespace FinanceManager.Domain.Repositories;

public sealed record TransactionFilter(
    Guid? FinancialAccountId,
    Guid? CreditCardId,
    Guid? CategoryId,
    TransactionType? Type,
    DateOnly? DateFrom,
    DateOnly? DateTo);
