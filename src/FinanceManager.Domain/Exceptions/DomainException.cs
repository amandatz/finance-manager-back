namespace FinanceManager.Domain.Exceptions;

/// <summary>
/// Base type for exceptions representing an expected, client-facing outcome of a business rule,
/// mapped by the global exception handler to a specific HTTP status.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message)
        : base(message)
    {
    }

    protected DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
