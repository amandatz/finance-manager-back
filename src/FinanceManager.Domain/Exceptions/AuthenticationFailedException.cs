namespace FinanceManager.Domain.Exceptions;

/// <summary>
/// Thrown when presented credentials are rejected. Carries a deliberately generic message -
/// callers must never learn which part was wrong. Maps to HTTP 401.
/// </summary>
public sealed class AuthenticationFailedException : DomainException
{
    public AuthenticationFailedException(string message)
        : base(message)
    {
    }
}
