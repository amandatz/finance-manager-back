namespace FinanceManager.Application.CreditCards.Requests;

public sealed record UpdateCreditCardRequest(
    decimal CreditLimit,
    int StatementClosingDay,
    int PaymentDueDay,
    string? Brand,
    Guid? SettlementAccountId);
