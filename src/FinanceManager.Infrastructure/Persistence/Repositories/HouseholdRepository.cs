using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Repositories;
using FinanceManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinanceManager.Infrastructure.Persistence.Repositories;

public sealed class HouseholdRepository(FinanceManagerDbContext context) : IHouseholdRepository
{
    public async Task<Household?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.Households.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<Household?> GetByInviteCodeAsync(string inviteCode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(inviteCode))
        {
            return null;
        }

        return await context.Households
            .FirstOrDefaultAsync(x => x.InviteCode == inviteCode, cancellationToken);
    }

    public async Task AddAsync(Household household, CancellationToken cancellationToken)
    {
        await context.Households.AddAsync(household, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await context.SaveChangesAsync(cancellationToken);
    }
}
