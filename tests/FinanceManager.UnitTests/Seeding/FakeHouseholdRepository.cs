using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Repositories;

namespace FinanceManager.UnitTests.Seeding;

internal sealed class FakeHouseholdRepository : IHouseholdRepository
{
    public bool AddCalled { get; private set; }

    public Task AddAsync(Household household, CancellationToken cancellationToken)
    {
        AddCalled = true;
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
