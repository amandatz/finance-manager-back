namespace FinanceManager.Application.Abstractions;

public sealed record CreateUserResult(
    CreateUserOutcome Outcome,
    Guid? UserId,
    IReadOnlyList<string> Errors);
