using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Repositories;
using FinanceManager.Infrastructure.Persistence;

namespace FinanceManager.Infrastructure.Persistence.Repositories;

public sealed class HouseholdRepository(FinanceManagerDbContext context) : IHouseholdRepository
{
    public async Task AddAsync(Household household, CancellationToken cancellationToken)
    {
        await context.Households.AddAsync(household, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await context.SaveChangesAsync(cancellationToken);
    }
}
