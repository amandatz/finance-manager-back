namespace FinanceManager.Domain.Exceptions;

/// <summary>
/// Thrown when a request is well-formed (passes shape/type validation) but is rejected by
/// a business rule - e.g. an incorrect current password, or a new password that fails the
/// password policy. Distinct from <see cref="DomainValidationException"/>, which signals
/// an entity invariant violation rather than a policy/workflow rejection. Maps to HTTP 400.
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
    /// One or more human-readable reasons the rule was violated (e.g. individual password
    /// policy failures). Always contains at least one entry.
    /// </summary>
    public IReadOnlyList<string> Errors { get; }
}
