using FinanceManager.Application.FinancialAccounts;
using FinanceManager.Application.Seeding;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinanceManager.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ValidatorOptions.Global.LanguageManager.Enabled = false;

        services.AddValidatorsFromAssemblyContaining(typeof(DependencyInjection));

        services.AddScoped<FinancialAccountsService>();

        services.Configure<SeedOptions>(configuration.GetSection("Seed"));
        services.AddScoped<HouseholdSeeder>();

        return services;
    }
}
