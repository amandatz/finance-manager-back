using FinanceManager.Domain.Entities;

namespace FinanceManager.Domain.Repositories;

public interface ICreditCardRepository
{
    Task<CreditCard?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<CreditCard>> GetAllAsync(
        CancellationToken cancellationToken);

    Task AddAsync(
        CreditCard creditCard,
        CancellationToken cancellationToken);

    void Remove(CreditCard creditCard);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
