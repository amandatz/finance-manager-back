namespace FinanceManager.Domain.Exceptions;

/// <summary>
/// Thrown when a requested entity does not exist or is not visible to the caller. Maps to HTTP 404.
/// </summary>
public sealed class NotFoundException : DomainException
{
    public NotFoundException(string message)
        : base(message)
    {
    }

    public static NotFoundException For(string entityName, Guid id) =>
        new($"{entityName} with id '{id}' was not found.");
}
