using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FinanceManager.IntegrationTests;

public class ThrowingFinancialAccountsApiFactory : InMemoryDbApiFactory
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.RemoveAll<IFinancialAccountRepository>();
        services.AddScoped<IFinancialAccountRepository, ThrowingFinancialAccountRepository>();
    }

    private sealed class ThrowingFinancialAccountRepository : IFinancialAccountRepository
    {
        public Task<FinancialAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Simulated failure.");

        public Task<IReadOnlyList<FinancialAccount>> GetAllAsync(CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Simulated failure.");

        public Task AddAsync(FinancialAccount account, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Simulated failure.");

        public void Remove(FinancialAccount account) =>
            throw new InvalidOperationException("Simulated failure.");

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Simulated failure.");
    }
}
