using FinanceManager.Application.Seeding;
using FinanceManager.Domain.Enums;
using FinanceManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FinanceManager.IntegrationTests.Users;

public class SeedingTests : IDisposable
{
    private readonly UserHouseholdApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Seed_WithEmptyDatabaseAndValidConfig_CreatesHouseholdAndOwner()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinanceManagerDbContext>();

        var household = Assert.Single(await db.Households.ToListAsync());
        var user = Assert.Single(await db.Users.ToListAsync());

        Assert.Equal(UserHouseholdApiFactory.SeedHouseholdName, household.Name);
        Assert.Equal(UserHouseholdApiFactory.SeedOwnerEmail, user.Email);
        Assert.Equal(household.Id, user.HouseholdId);
        Assert.Equal(HouseholdRole.Owner, user.Role);
    }

    [Fact]
    public async Task Seed_WithExistingUser_DoesNothing()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinanceManagerDbContext>();
        var initialHouseholdCount = await db.Households.CountAsync();
        var initialUserCount = await db.Users.CountAsync();

        var seeder = scope.ServiceProvider.GetRequiredService<HouseholdSeeder>();
        await seeder.SeedAsync(CancellationToken.None);

        Assert.Equal(initialHouseholdCount, await db.Households.CountAsync());
        Assert.Equal(initialUserCount, await db.Users.CountAsync());
    }
}
