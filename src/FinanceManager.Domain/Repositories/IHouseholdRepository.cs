using FinanceManager.Domain.Entities;

namespace FinanceManager.Domain.Repositories;

public interface IHouseholdRepository
{
    Task AddAsync(Household household, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
