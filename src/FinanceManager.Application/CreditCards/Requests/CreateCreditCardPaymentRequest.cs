namespace FinanceManager.Application.CreditCards.Requests;

public sealed record CreateCreditCardPaymentRequest(
    Guid FromFinancialAccountId,
    decimal Amount,
    DateOnly Date,
    string? Description,
    string? Currency = null);
