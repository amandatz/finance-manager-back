using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Repositories;
using FinanceManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinanceManager.Infrastructure.Persistence.Repositories;

public sealed class FinancialAccountRepository(
    FinanceManagerDbContext context) : IFinancialAccountRepository
{
    public async Task<FinancialAccount?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await context.FinancialAccounts
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyList<FinancialAccount>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        return await context.FinancialAccounts
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        FinancialAccount account,
        CancellationToken cancellationToken)
    {
        await context.FinancialAccounts.AddAsync(
            account,
            cancellationToken);
    }

    public void Remove(FinancialAccount account)
    {
        context.FinancialAccounts.Remove(account);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        await context.SaveChangesAsync(cancellationToken);
    }
}