using FinanceManager.Domain.Entities;
using FinanceManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FinanceManager.IntegrationTests.CreditCards;

public class CreditCardsApiFactory : UserHouseholdApiFactory
{
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
