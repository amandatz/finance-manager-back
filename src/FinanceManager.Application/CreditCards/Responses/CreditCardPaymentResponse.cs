using FinanceManager.Application.Transactions.Responses;

namespace FinanceManager.Application.CreditCards.Responses;

public sealed record CreditCardPaymentResponse(
    Guid SettlementGroupId,
    TransactionResponse AccountLeg,
    TransactionResponse CreditCardLeg);
