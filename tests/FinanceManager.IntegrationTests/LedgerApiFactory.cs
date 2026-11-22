using FinanceManager.Domain.Entities;
using FinanceManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FinanceManager.IntegrationTests;

/// <summary>
/// Combines household/auth test helpers with seeding for the ledger entities
/// (<see cref="Category"/>, <see cref="Transaction"/>, <see cref="FinancialAccount"/>,
/// <see cref="CreditCard"/>) - seeding always goes straight through the DbContext, never
/// through the filtered repository.
/// </summary>
public class LedgerApiFactory : UserHouseholdApiFactory
{
    public async Task<Category> SeedCategoryAsync(Category category)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinanceManagerDbContext>();

        db.Categories.Add(category);
        await db.SaveChangesAsync();

        return category;
    }

    public async Task<Category?> FindCategoryAsync(Guid id)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinanceManagerDbContext>();

        return await db.Categories.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<FinancialAccount> SeedAccountAsync(FinancialAccount account)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinanceManagerDbContext>();

        db.FinancialAccounts.Add(account);
        await db.SaveChangesAsync();

        return account;
    }

    public async Task<Transaction> SeedTransactionAsync(Transaction transaction)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinanceManagerDbContext>();

        db.Transactions.Add(transaction);
        await db.SaveChangesAsync();

        return transaction;
    }

    public async Task<Transaction?> FindTransactionAsync(Guid id)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinanceManagerDbContext>();

        return await db.Transactions.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<CreditCard> SeedCreditCardAsync(CreditCard creditCard)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinanceManagerDbContext>();

        db.CreditCards.Add(creditCard);
        await db.SaveChangesAsync();

        return creditCard;
    }

    public async Task<CreditCard?> FindCreditCardAsync(Guid id)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinanceManagerDbContext>();

        return await db.CreditCards.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id);
    }
}
