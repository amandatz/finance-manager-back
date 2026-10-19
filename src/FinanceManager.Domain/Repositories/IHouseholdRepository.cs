using FinanceManager.Domain.Entities;

namespace FinanceManager.Domain.Repositories;

public interface IHouseholdRepository
{
    Task<Household?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Household?> GetByInviteCodeAsync(string inviteCode, CancellationToken cancellationToken);

    Task AddAsync(Household household, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
