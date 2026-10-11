namespace FinanceManager.Domain.Exceptions;

/// <summary>
/// Thrown when an operation cannot complete because it collides with existing state
/// (e.g. a unique constraint such as "email already in use"). Maps to HTTP 409.
/// </summary>
public sealed class ConflictException : DomainException
{
    public ConflictException(string message)
        : base(message)
    {
    }
}
