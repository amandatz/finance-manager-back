using FinanceManager.Application.CreditCards;
using FinanceManager.Application.FinancialAccounts;
using FinanceManager.Application.Households;
using FinanceManager.Application.Users;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace FinanceManager.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ValidatorOptions.Global.LanguageManager.Enabled = false;

        services.AddValidatorsFromAssemblyContaining(typeof(DependencyInjection));

        services.AddScoped<FinancialAccountsService>();
        services.AddScoped<CreditCardsService>();
        services.AddScoped<AuthService>();
        services.AddScoped<UsersService>();
        services.AddScoped<HouseholdsService>();

        return services;
    }
}
