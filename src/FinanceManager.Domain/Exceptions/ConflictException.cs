namespace FinanceManager.Domain.Exceptions;

/// <summary>
/// Thrown when an operation collides with existing state. Maps to HTTP 409.
/// </summary>
public sealed class ConflictException : DomainException
{
    public ConflictException(string message)
        : base(message)
    {
    }
}
