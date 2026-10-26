namespace FinanceManager.Domain.Exceptions;

/// <summary>
/// Thrown when a well-formed request is rejected by a business rule, as opposed to an entity
/// invariant (<see cref="DomainValidationException"/>). Maps to HTTP 400.
/// </summary>
public sealed class BusinessRuleViolationException : DomainException
{
    public BusinessRuleViolationException(string message)
        : base(message)
    {
        Errors = [message];
    }

    public BusinessRuleViolationException(string message, IReadOnlyList<string> errors)
        : base(message)
    {
        Errors = errors.Count > 0 ? errors : [message];
    }

    /// <summary>
    /// Human-readable reasons the rule was violated. Always contains at least one entry.
    /// </summary>
    public IReadOnlyList<string> Errors { get; }
}
