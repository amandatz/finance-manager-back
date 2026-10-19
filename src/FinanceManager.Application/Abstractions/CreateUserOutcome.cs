namespace FinanceManager.Application.Abstractions;

public enum CreateUserOutcome
{
    Success,
    EmailAlreadyInUse,
    PasswordPolicyViolation,
}
