namespace FinanceManager.Application.CreditCards.Responses;

public sealed record CreditCardResponse(
    Guid Id,
    string? Brand,
    decimal CreditLimit,
    int StatementClosingDay,
    int PaymentDueDay,
    Guid? SettlementAccountId,
    IReadOnlyCollection<Guid> OwnerUserIds,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
