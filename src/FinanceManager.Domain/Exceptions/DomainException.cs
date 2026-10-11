namespace FinanceManager.Domain.Exceptions;

/// <summary>
/// Base type for every exception that represents an expected, client-facing outcome of a
/// business rule or invariant - as opposed to a programming error or an unexpected
/// infrastructure failure. The API's global exception handler maps each subclass of this
/// type to a specific HTTP status code instead of a generic 500, so the exception itself
/// is the single source of truth for "what went wrong" - controllers and services no
/// longer need to translate outcome enums or null checks into status codes by hand.
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
