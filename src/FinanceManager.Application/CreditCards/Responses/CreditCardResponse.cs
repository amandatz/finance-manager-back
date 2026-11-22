using FinanceManager.Domain.Enums;

namespace FinanceManager.Application.CreditCards.Responses;

public sealed record CreditCardResponse(
    Guid Id,
    string? Brand,
    decimal CreditLimit,
    int StatementClosingDay,
    int PaymentDueDay,
    Guid? SettlementAccountId,
    CreditCardColor Color,
    IReadOnlyCollection<Guid> OwnerUserIds,
    decimal CurrentDebt,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
