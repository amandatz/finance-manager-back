using FinanceManager.Application.Abstractions;
using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;
using FinanceManager.Domain.Repositories;
using Microsoft.Extensions.Options;

namespace FinanceManager.Application.Seeding;

public sealed class HouseholdSeeder(
    IIdentityService identityService,
    IHouseholdRepository householdRepository,
    IOptions<SeedOptions> seedOptions)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (await identityService.AnyUsersExistAsync(cancellationToken))
        {
            return;
        }

        var options = seedOptions.Value;
        var missingKeys = new List<string>();

        if (string.IsNullOrWhiteSpace(options.OwnerEmail))
        {
            missingKeys.Add("Seed:OwnerEmail");
        }

        if (string.IsNullOrWhiteSpace(options.OwnerPassword))
        {
            missingKeys.Add("Seed:OwnerPassword");
        }

        if (string.IsNullOrWhiteSpace(options.OwnerName))
        {
            missingKeys.Add("Seed:OwnerName");
        }

        if (string.IsNullOrWhiteSpace(options.HouseholdName))
        {
            missingKeys.Add("Seed:HouseholdName");
        }

        if (missingKeys.Count > 0)
        {
            throw new InvalidOperationException(
                $"Cannot seed the initial household: missing configuration {string.Join(", ", missingKeys)}.");
        }

        var household = new Household(options.HouseholdName!);
        await householdRepository.AddAsync(household, cancellationToken);
        await householdRepository.SaveChangesAsync(cancellationToken);

        var result = await identityService.CreateUserAsync(
            options.OwnerName!,
            options.OwnerEmail!,
            options.OwnerPassword!,
            household.Id,
            HouseholdRole.Owner,
            cancellationToken);

        if (result.Outcome != CreateUserOutcome.Success)
        {
            throw new InvalidOperationException(
                $"Failed to seed the initial owner: {string.Join(", ", result.Errors)}");
        }
    }
}
