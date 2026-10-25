using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FinanceManager.Infrastructure.Persistence.Repositories;

public sealed class CreditCardRepository(
    FinanceManagerDbContext context) : ICreditCardRepository
{
    public async Task<CreditCard?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await context.CreditCards
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<CreditCard>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        return await context.CreditCards
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        CreditCard creditCard,
        CancellationToken cancellationToken)
    {
        await context.CreditCards.AddAsync(creditCard, cancellationToken);
    }

    public void Remove(CreditCard creditCard)
    {
        context.CreditCards.Remove(creditCard);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await context.SaveChangesAsync(cancellationToken);
    }
}
