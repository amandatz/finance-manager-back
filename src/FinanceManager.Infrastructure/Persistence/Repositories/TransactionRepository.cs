using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;
using FinanceManager.Domain.Repositories;
using FinanceManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinanceManager.Infrastructure.Persistence.Repositories;

public sealed class TransactionRepository(
    FinanceManagerDbContext context) : ITransactionRepository
{
    public async Task<Transaction?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await context.Transactions
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Transaction>> GetAllAsync(
        TransactionFilter filter,
        CancellationToken cancellationToken)
    {
        var query = context.Transactions.AsQueryable();

        if (filter.FinancialAccountId is { } financialAccountId)
        {
            query = query.Where(x => x.FinancialAccountId == financialAccountId);
        }

        if (filter.CreditCardId is { } creditCardId)
        {
            query = query.Where(x => x.CreditCardId == creditCardId);
        }

        if (filter.CategoryId is { } categoryId)
        {
            query = query.Where(x => x.CategoryId == categoryId);
        }

        if (filter.Type is { } type)
        {
            query = query.Where(x => x.Type == type);
        }

        if (filter.DateFrom is { } dateFrom)
        {
            query = query.Where(x => x.Date >= dateFrom);
        }

        if (filter.DateTo is { } dateTo)
        {
            query = query.Where(x => x.Date <= dateTo);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<decimal> GetAccountBalanceAsync(
        Guid financialAccountId,
        CancellationToken cancellationToken)
    {
        return await context.Transactions
            .Where(x => x.FinancialAccountId == financialAccountId && x.IsActive)
            .SumAsync(
                x => x.Type == TransactionType.Income ? x.Amount : -x.Amount,
                cancellationToken);
    }

    public async Task<decimal> GetCreditCardDebtAsync(
        Guid creditCardId,
        CancellationToken cancellationToken)
    {
        return await context.Transactions
            .Where(x => x.CreditCardId == creditCardId && x.IsActive)
            .SumAsync(
                x => x.Type == TransactionType.Expense ? x.Amount : -x.Amount,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Transaction>> GetBySettlementGroupIdAsync(
        Guid settlementGroupId,
        CancellationToken cancellationToken)
    {
        return await context.Transactions
            .Where(x => x.SettlementGroupId == settlementGroupId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        Transaction transaction,
        CancellationToken cancellationToken)
    {
        await context.Transactions.AddAsync(
            transaction,
            cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        await context.SaveChangesAsync(cancellationToken);
    }
}
