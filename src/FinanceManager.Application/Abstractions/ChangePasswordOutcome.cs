namespace FinanceManager.Application.Abstractions;

public enum ChangePasswordOutcome
{
    Success,
    CurrentPasswordIncorrect,
    NewPasswordPolicyViolation,
}
