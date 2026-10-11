namespace FinanceManager.Domain.Exceptions;

/// <summary>
/// Thrown when credentials presented by the caller (e.g. email/password at login, or a
/// token that no longer resolves to a user) are rejected. Deliberately carries a generic
/// message - callers must never be told whether the email or the password was the
/// specific problem. Maps to HTTP 401.
/// </summary>
public sealed class AuthenticationFailedException : DomainException
{
    public AuthenticationFailedException(string message)
        : base(message)
    {
    }
}
