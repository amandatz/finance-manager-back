using FinanceManager.Domain.Enums;

namespace FinanceManager.Application.CreditCards.Requests;

public sealed record CreateCreditCardRequest(
    decimal CreditLimit,
    int StatementClosingDay,
    int PaymentDueDay,
    string? Brand,
    Guid? SettlementAccountId,
    CreditCardColor Color = CreditCardColor.Graphite);
