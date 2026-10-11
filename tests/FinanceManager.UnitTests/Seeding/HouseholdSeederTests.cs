using FinanceManager.Application.Seeding;
using Microsoft.Extensions.Options;

namespace FinanceManager.UnitTests.Seeding;

public class HouseholdSeederTests
{
    [Fact]
    public async Task SeedAsync_WithMissingOwnerEmail_ThrowsNamingTheKey()
    {
        var identityService = new FakeIdentityService { UsersExist = false };
        var householdRepository = new FakeHouseholdRepository();
        var options = Options.Create(new SeedOptions
        {
            OwnerEmail = null,
            OwnerPassword = "SomePassword123",
            OwnerName = "Owner",
            HouseholdName = "Household",
        });
        var seeder = new HouseholdSeeder(identityService, householdRepository, options);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => seeder.SeedAsync(CancellationToken.None));

        Assert.Contains("Seed:OwnerEmail", exception.Message);
    }

    [Fact]
    public async Task SeedAsync_WithMissingConfig_CreatesNoHousehold()
    {
        var identityService = new FakeIdentityService { UsersExist = false };
        var householdRepository = new FakeHouseholdRepository();
        var options = Options.Create(new SeedOptions
        {
            OwnerEmail = null,
            OwnerPassword = "SomePassword123",
            OwnerName = "Owner",
            HouseholdName = "Household",
        });
        var seeder = new HouseholdSeeder(identityService, householdRepository, options);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => seeder.SeedAsync(CancellationToken.None));

        Assert.False(householdRepository.AddCalled);
        Assert.False(identityService.CreateUserCalled);
    }
}
