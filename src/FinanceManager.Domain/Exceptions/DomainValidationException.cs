namespace FinanceManager.Domain.Exceptions;

/// <summary>
/// Thrown when an entity invariant is violated while constructing or mutating a domain
/// entity (e.g. a name that is empty or exceeds its maximum length). Raised from inside
/// the domain model itself, so it carries less context than FluentValidation's
/// request-level checks - but it must still reach the client as a 400, not a 500, since it
/// is directly caused by caller-supplied input. Maps to HTTP 400.
/// </summary>
public sealed class DomainValidationException : DomainException
{
    public DomainValidationException(string message, string? propertyName = null)
        : base(message)
    {
        PropertyName = propertyName;
    }

    /// <summary>
    /// The name of the invalid member, if known. Null when the violation spans multiple
    /// members or doesn't map cleanly to one.
    /// </summary>
    public string? PropertyName { get; }
}
