namespace FinanceManager.Domain.Exceptions;

/// <summary>
/// Thrown when a domain entity invariant is violated during construction or mutation. Maps to HTTP 400.
/// </summary>
public sealed class DomainValidationException : DomainException
{
    public DomainValidationException(string message, string? propertyName = null)
        : base(message)
    {
        PropertyName = propertyName;
    }

    /// <summary>
    /// The invalid member's name, if known; null when the violation doesn't map to a single member.
    /// </summary>
    public string? PropertyName { get; }
}
